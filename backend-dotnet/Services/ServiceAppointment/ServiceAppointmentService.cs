using backend_dotnet.Data;
using backend_dotnet.Models.Domain;
using backend_dotnet.Models.DTOs.Appointment;
using backend_dotnet.Models.DTOs.ServiceAppointment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Razorpay.Api;
using System.Net;
using System.Text.RegularExpressions;

namespace backend_dotnet.Services.ServiceAppointment
{
    public class ServiceAppointmentService : IServiceAppointmentService
    {
        private readonly ApplicationDbContext _db;
        private readonly string _razorpayKeyId;
        private readonly string _razorpayKeySecret;
        private readonly string _frontendUrl;

        public ServiceAppointmentService(ApplicationDbContext db, IConfiguration configuration)
        {
            _db = db;
            _razorpayKeyId = configuration["Razorpay:KeyId"] ?? "";
            _razorpayKeySecret = configuration["Razorpay:KeySecret"] ?? "";
            _frontendUrl = configuration["App:FrontendUrl"] ?? "http://localhost:5173";
        }

        //--------------------------------GetServiceAppointmentsAsync--------------------------------------------------------

        public async Task<ServiceAppointmentListResultDTO> GetServiceAppointmentsAsync(GetServiceAppointmentsQueryDTO query)
        {
            var limit = Math.Min(200, Math.Max(1, query.Limit ?? 50));
            var page = Math.Max(1, query.Page ?? 1);
            var skip = (page - 1) * limit;

            var appointmentsQuery = _db.ServiceAppointments.Where(a => a.IsActive && !a.IsDeleted && a.Service.IsActive && !a.Service.IsDeleted).AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.ServiceId) && Guid.TryParse(query.ServiceId, out var serviceGuid))
                appointmentsQuery = appointmentsQuery.Where(a => a.ServiceId == serviceGuid);

            if (!string.IsNullOrWhiteSpace(query.Mobile))
                appointmentsQuery = appointmentsQuery.Where(a => a.Mobile == query.Mobile);

            if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<AppointmentStatus>(query.Status, true, out var statusEnum))
                appointmentsQuery = appointmentsQuery.Where(a => a.Status == statusEnum);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search;
                appointmentsQuery = appointmentsQuery.Where(a =>
                    EF.Functions.ILike(a.PatientName, $"%{search}%") ||
                    EF.Functions.ILike(a.Mobile, $"%{search}%"));
            }

            var total = await appointmentsQuery.CountAsync();

            var list = await appointmentsQuery
                .OrderByDescending(a => a.Date)
                .ThenByDescending(a => a.CreatedAt)
                .Skip(skip)
                .Take(limit)
                .ToListAsync();

            return new ServiceAppointmentListResultDTO
            {
                IsSuccess = true,
                Appointments = list.Cast<object>().ToList(),
                Total = total,
                Page = page,
                Limit = limit,
                Count = list.Count
            };
        }

        //--------------------------------GetServiceAppointmentByIdAsync--------------------------------------------------------

        public async Task<ServiceAppointmentResultDTO> GetServiceAppointmentByIdAsync(Guid id)
        {
            var appt = await _db.ServiceAppointments.FindAsync(id);

            if (appt is null)
            {
                return new ServiceAppointmentResultDTO
                {
                    IsSuccess = false,
                    ErrorMessage = "Not found"
                };
            }

            return new ServiceAppointmentResultDTO
            {
                IsSuccess = true,
                Data = appt
            };
        }

        //--------------------------------------------------------CreateServiceAppointmentAsync--------------

        public async Task<ServiceAppointmentCreateResultDTO> CreateServiceAppointmentAsync(
            CreateServiceAppointmentRequestDTO body,
            string? authenticatedUserId,
            string? frontendOrigin)
        {
            if (string.IsNullOrWhiteSpace(authenticatedUserId))
            {
                return new ServiceAppointmentCreateResultDTO
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.Unauthorized,
                    ErrorMessage = "Authentication required to create a service appointment."
                };
            }

            if (string.IsNullOrWhiteSpace(body.ServiceId))
                return Bad("serviceId is required");

            if (string.IsNullOrWhiteSpace(body.PatientName))
                return Bad("patientName is required");

            if (string.IsNullOrWhiteSpace(body.Mobile))
                return Bad("mobile is required");

            if (string.IsNullOrWhiteSpace(body.Date))
                return Bad("date is required (YYYY-MM-DD)");

            var numericAmount = body.Amount ?? body.Fees ?? 0;
            if (numericAmount < 0)
                return Bad("amount/fees must be a valid number");

            int? finalHour = body.Hour;
            int? finalMinute = body.Minute;
            string? finalAmpm = body.AmPm;

            if (!string.IsNullOrWhiteSpace(body.Time) && finalHour is null)
            {
                var parsed = ParseTimeString(body.Time);
                if (parsed is null)
                    return Bad("time string couldn't be parsed");

                finalHour = parsed.Value.Hour;
                finalMinute = parsed.Value.Minute;
                finalAmpm = parsed.Value.AmPm;
            }

            if (finalHour is null || finalMinute is null || (finalAmpm != "AM" && finalAmpm != "PM"))
            {
                return Bad("Time missing or invalid — provide time string or hour, minute and ampm.");
            }

            if (!Guid.TryParse(body.ServiceId, out var serviceIdGuid))
            {
                return Bad("Invalid serviceId GUID");
            }

            // DUPLICATE BOOKING CHECK
            try
            {
                var existing = await _db.ServiceAppointments.FirstOrDefaultAsync(a =>
                    a.ServiceId == serviceIdGuid &&
                    a.CreatedBy == authenticatedUserId &&
                    a.Date == body.Date &&
                    a.Hour == finalHour.Value &&
                    a.Minute == finalMinute.Value &&
                    a.Ampm == finalAmpm &&
                    a.Status != AppointmentStatus.Canceled);

                if (existing is not null)
                {
                    return new ServiceAppointmentCreateResultDTO
                    {
                        IsSuccess = false,
                        StatusCode = HttpStatusCode.Conflict,
                        ErrorMessage = "You already have a booking for this service at the selected date and time."
                    };
                }
            }
            catch (Exception chkErr)
            {
                Console.Error.WriteLine($"Duplicate booking check failed: {chkErr.Message}");
            }

            // Fetch service snapshot (non-fatal)
            backend_dotnet.Models.Domain.Service? svc = null;
            try
            {
                svc = await _db.Services.FirstOrDefaultAsync(s => s.Id == serviceIdGuid);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Service lookup failed: {e.Message}");
            }

            var resolvedServiceName = !string.IsNullOrWhiteSpace(body.ServiceName)
                ? body.ServiceName
                : (svc?.Name ?? "Service");

            var svcImageUrlFromDb = svc?.ImageUrl?.Trim() ?? "";
            var svcImagePublicIdFromDb = svc?.ImagePublicId?.Trim() ?? "";

            var finalServiceImageUrl = !string.IsNullOrEmpty(svcImageUrlFromDb)
                ? svcImageUrlFromDb
                : (body.ServiceImageUrl?.Trim() ?? "");

            var finalServiceImagePublicId = !string.IsNullOrEmpty(svcImagePublicIdFromDb)
                ? svcImagePublicIdFromDb
                : (body.ServiceImagePublicId?.Trim() ?? "");

            var appointment = new backend_dotnet.Models.Domain.ServiceAppointment
            {
                ServiceId = serviceIdGuid,
                ServiceName = resolvedServiceName,
                ServiceImageUrl = finalServiceImageUrl,
                ServiceImagePubId = finalServiceImagePublicId,
                PatientName = body.PatientName.Trim(),
                Mobile = body.Mobile.Trim(),
                Age = int.TryParse(body.Age, out var parsedAge) ? parsedAge : null,
                Gender = body.Gender ?? "",
                Date = body.Date,
                Hour = finalHour.Value,
                Minute = finalMinute.Value,
                Ampm = finalAmpm ?? "AM",
                Fees = numericAmount,
                CreatedBy = authenticatedUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Free appointment
            if (numericAmount == 0)
            {
                appointment.Status = AppointmentStatus.Pending;
                appointment.PaymentMethod = PaymentMethod.Cash;
                appointment.PaymentStatus = PaymentStatus.Paid;
                appointment.PaymentAmount = 0;
                appointment.PaidAt = DateTime.UtcNow;

                _db.ServiceAppointments.Add(appointment);
                await _db.SaveChangesAsync();

                return new ServiceAppointmentCreateResultDTO
                {
                    IsSuccess = true,
                    StatusCode = HttpStatusCode.Created,
                    Appointment = appointment
                };
            }

            // Cash booking
            if (body.PaymentMethod == "Cash")
            {
                appointment.Status = AppointmentStatus.Pending;
                appointment.PaymentMethod = PaymentMethod.Cash;
                appointment.PaymentStatus = PaymentStatus.Pending;
                appointment.PaymentAmount = numericAmount;

                _db.ServiceAppointments.Add(appointment);
                await _db.SaveChangesAsync();

                return new ServiceAppointmentCreateResultDTO
                {
                    IsSuccess = true,
                    StatusCode = HttpStatusCode.Created,
                    Appointment = appointment,
                    CheckoutUrl = null
                };
            }

            // Online booking (Razorpay)
            if (string.IsNullOrWhiteSpace(_razorpayKeyId) || string.IsNullOrWhiteSpace(_razorpayKeySecret))
            {
                return new ServiceAppointmentCreateResultDTO
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.InternalServerError,
                    ErrorMessage = "Razorpay not configured on server"
                };
            }

            try
            {
                appointment.Status = AppointmentStatus.Pending;
                appointment.PaymentMethod = PaymentMethod.Online;
                appointment.PaymentStatus = PaymentStatus.Pending;
                appointment.PaymentAmount = numericAmount;

                _db.ServiceAppointments.Add(appointment);
                await _db.SaveChangesAsync();

                // Create Razorpay Order
                var client = new RazorpayClient(_razorpayKeyId, _razorpayKeySecret);
                var options = new Dictionary<string, object>
                {
                    { "amount", (int)(numericAmount * 100) }, // Amount in paise
                    { "currency", "INR" },
                    { "receipt", appointment.Id.ToString() }
                };
                Order order = client.Order.Create(options);
                string orderId = order["id"].ToString();

                appointment.PaymentSessionId = orderId;
                await _db.SaveChangesAsync();

                return new ServiceAppointmentCreateResultDTO
                {
                    IsSuccess = true,
                    StatusCode = HttpStatusCode.Created,
                    Appointment = appointment,
                    RazorpayOrderId = orderId
                };
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Razorpay order creation or DB error: {ex.Message}");
                return new ServiceAppointmentCreateResultDTO
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.InternalServerError,
                    ErrorMessage = "Failed to create service appointment record or payment order"
                };
            }

            static ServiceAppointmentCreateResultDTO Bad(string msg) => new()
            {
                IsSuccess = false,
                StatusCode = HttpStatusCode.BadRequest,
                ErrorMessage = msg
            };
        }

        private string BuildFrontendBase(string? frontendOrigin)
        {
            if (!string.IsNullOrWhiteSpace(_frontendUrl))
                return _frontendUrl.TrimEnd('/');
            if (!string.IsNullOrWhiteSpace(frontendOrigin))
                return frontendOrigin.TrimEnd('/');
            return "http://localhost:5173";
        }



        //--------------------------------UpdateServiceAppointmentAsync--------------------------------------------------------

        public async Task<ServiceAppointmentUpdateResultDTO> UpdateServiceAppointmentAsync(Guid id, UpdateServiceAppointmentRequestDTO body)
        {
            var existing = await _db.ServiceAppointments.FindAsync(id);

            if (existing is null)
            {
                return new ServiceAppointmentUpdateResultDTO
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessage = "Not found"
                };
            }

            string? newStatus = null;

            if (body.Status is not null)
                newStatus = body.Status;

            if (body.Payment is not null)
            {
                if (body.Payment.Method is not null && Enum.TryParse<PaymentMethod>(body.Payment.Method, true, out var payMethod))
                    existing.PaymentMethod = payMethod;

                if (body.Payment.Status is not null && Enum.TryParse<PaymentStatus>(body.Payment.Status, true, out var payStatus))
                    existing.PaymentStatus = payStatus;

                if (body.Payment.Amount is not null)
                    existing.PaymentAmount = body.Payment.Amount.Value;

                if (body.Payment.PaidAt is not null)
                    existing.PaidAt = body.Payment.PaidAt;
            }

            if (body.RescheduledTo is not null)
            {
                if (!string.IsNullOrWhiteSpace(body.RescheduledTo.Date))
                {
                    if (!Regex.IsMatch(body.RescheduledTo.Date, @"^\d{4}-\d{2}-\d{2}$"))
                    {
                        return new ServiceAppointmentUpdateResultDTO
                        {
                            IsSuccess = false,
                            StatusCode = HttpStatusCode.BadRequest,
                            ErrorMessage = "rescheduledTo.date must be YYYY-MM-DD"
                        };
                    }

                    existing.RescheduledDate = body.RescheduledTo.Date;
                    existing.Date = body.RescheduledTo.Date;
                }

                if (!string.IsNullOrWhiteSpace(body.RescheduledTo.Time))
                {
                    var parsed = ParseTimeString(body.RescheduledTo.Time);
                    if (parsed is null)
                    {
                        return new ServiceAppointmentUpdateResultDTO
                        {
                            IsSuccess = false,
                            StatusCode = HttpStatusCode.BadRequest,
                            ErrorMessage = "rescheduledTo.time couldn't be parsed"
                        };
                    }

                    existing.Hour = parsed.Value.Hour;
                    existing.Minute = parsed.Value.Minute;
                    existing.Ampm = parsed.Value.AmPm;

                    existing.RescheduledHour = parsed.Value.Hour;
                    existing.RescheduledMinute = parsed.Value.Minute;
                    existing.RescheduledAmpm = parsed.Value.AmPm;
                }

                if (string.IsNullOrWhiteSpace(body.Status))
                    newStatus = "Rescheduled";
            }

            if (body.Payment is not null)
            {
                var method = body.Payment.Method;
                if (!string.IsNullOrWhiteSpace(method) && method.Equals("online", StringComparison.OrdinalIgnoreCase))
                    newStatus ??= "Confirmed";

                if (body.Payment.Status == "Confirmed")
                {
                    newStatus = "Confirmed";
                    if (existing.PaidAt is null)
                        existing.PaidAt = DateTime.UtcNow;
                }
            }

            if (newStatus is not null && Enum.TryParse<AppointmentStatus>(newStatus, true, out var statusEnum))
                existing.Status = statusEnum;

            existing.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return new ServiceAppointmentUpdateResultDTO
            {
                IsSuccess = true,
                StatusCode = HttpStatusCode.OK,
                Data = existing
            };
        }

        //--------------------------------CancelServiceAppointmentAsync--------------------------------------------------------

        public async Task<ServiceAppointmentCancelResultDTO> CancelServiceAppointmentAsync(Guid id)
        {
            var appointment = await _db.ServiceAppointments.FindAsync(id);
            if (appointment is null)
            {
                return new ServiceAppointmentCancelResultDTO
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessage = "Not found"
                };
            }

            if (appointment.Status == AppointmentStatus.Canceled)
            {
                return new ServiceAppointmentCancelResultDTO
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "Appointment is already canceled"
                };
            }

            if (appointment.Status == AppointmentStatus.Completed)
            {
                return new ServiceAppointmentCancelResultDTO
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "Cannot cancel a completed appointment"
                };
            }

            appointment.Status = AppointmentStatus.Canceled;
            appointment.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new ServiceAppointmentCancelResultDTO
            {
                IsSuccess = true,
                StatusCode = HttpStatusCode.OK,
                Data = appointment
            };
        }

        //--------------------------------GetServiceAppointmentStatsAsync--------------------------------------------------------

        public async Task<ServiceAppointmentStatsResultDTO> GetServiceAppointmentStatsAsync()
        {
            var allServices = await _db.Services.AsNoTracking().ToListAsync();
            var allAppointments = await _db.ServiceAppointments.AsNoTracking().ToListAsync();

            var resultList = new List<object>();

            foreach (var svc in allServices)
            {
                var apptsForSvc = allAppointments.Where(a => a.ServiceId == svc.Id).ToList();

                var total = apptsForSvc.Count;
                var completed = apptsForSvc.Count(a => a.Status == AppointmentStatus.Completed);
                var canceled = apptsForSvc.Count(a => a.Status == AppointmentStatus.Canceled);
                var confirmed = apptsForSvc.Count(a => a.Status == AppointmentStatus.Confirmed);
                var earning = apptsForSvc
                    .Where(a => a.Status == AppointmentStatus.Completed)
                    .Sum(a => svc.Price);

                resultList.Add(new
                {
                    _id = svc.Id,
                    name = svc.Name,
                    price = svc.Price,
                    total,
                    confirmed,
                    completed,
                    canceled,
                    earning
                });
            }

            return new ServiceAppointmentStatsResultDTO
            {
                IsSuccess = true,
                Services = resultList,
                TotalServices = resultList.Count
            };
        }

        //--------------------------------GetServiceAppointmentsByPatientAsync--------------------------------------------------------

        public async Task<ServiceAppointmentListByPatientResultDTO> GetServiceAppointmentsByPatientAsync(
        GetServiceAppointmentsByPatientQueryDTO query,
        string? authenticatedUserId)
        {
            var resolvedCreatedBy = !string.IsNullOrWhiteSpace(query.CreatedBy)
                ? query.CreatedBy
                : authenticatedUserId;

            if (string.IsNullOrWhiteSpace(resolvedCreatedBy) && string.IsNullOrWhiteSpace(query.Mobile))
            {
                return new ServiceAppointmentListByPatientResultDTO
                {
                    IsSuccess = true,
                    Data = new List<object>()
                };
            }

            var appointmentsQuery = _db.ServiceAppointments.Where(a => a.IsActive && !a.IsDeleted && a.Service.IsActive && !a.Service.IsDeleted).AsQueryable();

            if (!string.IsNullOrWhiteSpace(resolvedCreatedBy))
                appointmentsQuery = appointmentsQuery.Where(a => a.CreatedBy == resolvedCreatedBy);

            if (!string.IsNullOrWhiteSpace(query.Mobile))
                appointmentsQuery = appointmentsQuery.Where(a => a.Mobile == query.Mobile);

            var list = await appointmentsQuery
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return new ServiceAppointmentListByPatientResultDTO
            {
                IsSuccess = true,
                Data = list.Cast<object>().ToList()
            };
        }

        //--------------------------------Helper functions --------------------------------------------------------

        private static (int Hour, int Minute, string AmPm)? ParseTimeString(string time)
        {
            // Accepts formats like "2:30 PM", "14:30", "2:30pm"
            var match = Regex.Match(time.Trim(), @"^(\d{1,2}):(\d{2})\s*(AM|PM|am|pm)?$");
            if (!match.Success)
                return null;

            if (!int.TryParse(match.Groups[1].Value, out var hour)) return null;
            if (!int.TryParse(match.Groups[2].Value, out var minute)) return null;

            string ampm;

            if (match.Groups[3].Success)
            {
                ampm = match.Groups[3].Value.ToUpperInvariant();
                if (hour < 1 || hour > 12 || minute < 0 || minute > 59)
                    return null;
            }
            else
            {
                // 24-hour format provided, convert to 12-hour + AM/PM
                if (hour < 0 || hour > 23 || minute < 0 || minute > 59)
                    return null;

                ampm = hour >= 12 ? "PM" : "AM";
                hour = hour % 12;
                if (hour == 0) hour = 12;
            }

            return (hour, minute, ampm);
        }

        //-------------------------------VerifyServiceRazorpayPaymentAsync------------------------------------------------------
        public async Task<ServiceAppointmentCreateResultDTO> VerifyServiceRazorpayPaymentAsync(string razorpayOrderId, string razorpayPaymentId, string razorpaySignature)
        {
            if (string.IsNullOrWhiteSpace(razorpayOrderId) || string.IsNullOrWhiteSpace(razorpayPaymentId) || string.IsNullOrWhiteSpace(razorpaySignature))
            {
                return new ServiceAppointmentCreateResultDTO
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "Missing payment verification parameters"
                };
            }

            try
            {
                var attributes = new Dictionary<string, string>
                {
                    { "razorpay_payment_id", razorpayPaymentId },
                    { "razorpay_order_id", razorpayOrderId },
                    { "razorpay_signature", razorpaySignature }
                };

                Utils.verifyPaymentSignature(attributes);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Razorpay signature verification failed: {ex.Message}");
                return new ServiceAppointmentCreateResultDTO
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "Invalid payment signature"
                };
            }

            var appt = await _db.ServiceAppointments.FirstOrDefaultAsync(a => a.PaymentSessionId == razorpayOrderId);
            if (appt is null)
            {
                return new ServiceAppointmentCreateResultDTO
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessage = "Service appointment not found"
                };
            }

            if (appt.PaymentStatus != PaymentStatus.Paid)
            {
                appt.PaymentStatus = PaymentStatus.Paid;
                appt.PaymentProviderId = razorpayPaymentId;
                appt.Status = AppointmentStatus.Confirmed;
                appt.PaidAt = DateTime.UtcNow;
                appt.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();
            }

            return new ServiceAppointmentCreateResultDTO
            {
                IsSuccess = true,
                Appointment = appt
            };
        }
    }
}
