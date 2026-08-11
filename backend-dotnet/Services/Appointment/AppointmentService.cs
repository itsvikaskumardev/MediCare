using backend_dotnet.Data;
using backend_dotnet.Models.DTOs.Appointment;
using Microsoft.EntityFrameworkCore;
using System.Net;
using static System.Collections.Specialized.BitVector32;

using Razorpay.Api;
using Microsoft.Extensions.Configuration;
namespace backend_dotnet.Services.Appointment
{
    public class AppointmentService : IAppointmentService
    {
        private readonly ApplicationDbContext _db;
        private readonly string _majorAdminId;
        private readonly string _razorpayKeyId;
        private readonly string _razorpayKeySecret;
        private readonly string _frontendUrl;

        public AppointmentService(ApplicationDbContext db, IConfiguration configuration)
        {
            _db = db;
            _majorAdminId = configuration["App:MajorAdminId"] ?? "";
            _razorpayKeyId = configuration["Razorpay:KeyId"] ?? "";
            _razorpayKeySecret = configuration["Razorpay:KeySecret"] ?? "";
            _frontendUrl = configuration["App:FrontendUrl"] ?? "http://localhost:5173";
        }

        // Implementation of Appointment service methods will be defined here

        //-------------------------------GetAppointments------------------------------------------------------

        public async Task<AppointmentListResultDTO> GetAppointmentsAsync(GetAppointmentsQueryDTO query)
        {
            var limit = Math.Min(200, Math.Max(1, query.Limit ?? 50));
            var page = Math.Max(1, query.Page ?? 1);
            var skip = (page - 1) * limit;

            var appointmentsQuery = _db.Appointments.Where(a => a.IsActive && !a.IsDeleted && a.Doctor.IsActive && !a.Doctor.IsDeleted).AsQueryable();

            if (query.DoctorId.HasValue)
                appointmentsQuery = appointmentsQuery.Where(a => a.DoctorId == query.DoctorId.Value);

            if (!string.IsNullOrWhiteSpace(query.Mobile))
                appointmentsQuery = appointmentsQuery.Where(a => a.Mobile == query.Mobile);

            if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<AppointmentStatus>(query.Status, true, out var status))
                appointmentsQuery = appointmentsQuery.Where(a => a.Status == status);

            if (!string.IsNullOrWhiteSpace(query.PatientClerkId))
                appointmentsQuery = appointmentsQuery.Where(a => a.CreatedBy == query.PatientClerkId);

            if (!string.IsNullOrWhiteSpace(query.CreatedBy))
                appointmentsQuery = appointmentsQuery.Where(a => a.CreatedBy == query.CreatedBy);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search;
                appointmentsQuery = appointmentsQuery.Where(a =>
                    EF.Functions.ILike(a.PatientName, $"%{search}%") ||
                    EF.Functions.ILike(a.Mobile, $"%{search}%"));
            }

            var total = await appointmentsQuery.CountAsync();

            var items = await appointmentsQuery
                .OrderByDescending(a => a.CreatedAt)
                .Skip(skip)
                .Take(limit)
                .Include(a => a.Doctor)
                .ThenInclude(d => d.User)
                .Select(a => new
                {
                    a.Id,
                    a.DoctorId,
                    Doctor = a.Doctor == null ? null : new
                    {
                        a.Doctor.User.Name,
                        a.Doctor.Specialization,
                        a.Doctor.User.ImageUrl
                    },
                    a.Mobile,
                    a.Status,
                    a.PatientName,
                    a.Date,
                    a.Time,
                    a.Fees,
                    a.CreatedBy,
                    a.CreatedAt
                })
                .ToListAsync();

            return new AppointmentListResultDTO
            {
                IsSuccess = true,
                Appointments = items.Cast<object>().ToList(),
                Page = page,
                Limit = limit,
                Total = total,
                Count = items.Count
            };
        }

        //-------------------------------GetAppointments------------------------------------------------------

        public async Task<AppointmentResultDTO> GetAppointmentByIdAsync(Guid id)
        {
            var appt = await _db.Appointments
                .AsNoTracking()
                .Include(a => a.Doctor)
                .ThenInclude(d => d.User)
                .Where(a => a.Id == id)
                .Select(a => new
                {
                    a.Id,
                    a.DoctorId,
                    Doctor = a.Doctor == null ? null : new
                    {
                        a.Doctor.User.Name,
                        a.Doctor.Specialization,
                        a.Doctor.User.ImageUrl
                    },
                    a.Mobile,
                    a.Status,
                    a.PatientName,
                    a.Date,
                    a.Time,
                    a.Fees,
                    a.PaymentMethod,
                    a.PaymentStatus,
                    a.PaymentAmount,
                    a.RescheduledDate,
                    a.RescheduledTime,
                    a.CreatedBy,
                    a.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (appt is null)
            {
                return new AppointmentResultDTO
                {
                    IsSuccess = false,
                    ErrorMessage = "Appointment not found"
                };
            }

            return new AppointmentResultDTO
            {
                IsSuccess = true,
                Appointment = appt
            };
        }

        //-------------------------------GetAppointmentsByPatient------------------------------------------------------
        public async Task<AppointmentListByPatientResultDTO> GetAppointmentsByPatientAsync(
        GetAppointmentsByPatientQueryDTO query,
        string? authenticatedUserId)
        {
            var resolvedCreatedBy = !string.IsNullOrWhiteSpace(query.CreatedBy)
                ? query.CreatedBy
                : authenticatedUserId;

            Console.WriteLine($"resolvedCreatedBy (query or authenticated user): {resolvedCreatedBy}");

            if (string.IsNullOrWhiteSpace(resolvedCreatedBy) && string.IsNullOrWhiteSpace(query.Mobile))
            {
                return new AppointmentListByPatientResultDTO
                {
                    IsSuccess = false,
                    IsAuthError = true,
                    ErrorMessage = "Authentication required for /me (no authenticated user detected on server). " +
                                   "Try passing ?createdBy=<id> to debug or check Authorization header forwarding."
                };
            }

            var appointmentsQuery = _db.Appointments.AsNoTracking().Where(a => a.IsActive && !a.IsDeleted && a.Doctor.IsActive && !a.Doctor.IsDeleted).Include(a => a.Doctor).ThenInclude(d => d.User).AsQueryable();

            if (!string.IsNullOrWhiteSpace(resolvedCreatedBy))
                appointmentsQuery = appointmentsQuery.Where(a => a.CreatedBy == resolvedCreatedBy);

            if (!string.IsNullOrWhiteSpace(query.Mobile))
                appointmentsQuery = appointmentsQuery.Where(a => a.Mobile == query.Mobile);

            var items = await appointmentsQuery
                .OrderBy(a => a.Date)
                .ThenBy(a => a.Time)
                .ToListAsync();

            return new AppointmentListByPatientResultDTO
            {
                IsSuccess = true,
                Appointments = items.Cast<object>().ToList()
            };
        }

        //-------------------------------CreateAppointment------------------------------------------------------


        public async Task<AppointmentCreateResultDTO> CreateAppointmentAsync(
            CreateAppointmentRequestDTO request,
            string? authenticatedUserId,
            string? frontendOrigin)
        {
            if (string.IsNullOrWhiteSpace(authenticatedUserId))
            {
                return new AppointmentCreateResultDTO
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.Unauthorized,
                    ErrorMessage = "Authentication required"
                };
            }

            if (string.IsNullOrWhiteSpace(request.DoctorId) ||
                string.IsNullOrWhiteSpace(request.PatientName) ||
                string.IsNullOrWhiteSpace(request.Mobile) ||
                string.IsNullOrWhiteSpace(request.Date) ||
                string.IsNullOrWhiteSpace(request.Time))
            {
                return new AppointmentCreateResultDTO
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "doctorId, patientName, mobile, date and time are required"
                };
            }

            var numericFee = SafeNumber(request.Fee ?? request.Fees ?? 0);
            if (numericFee is null || numericFee < 0)
            {
                return new AppointmentCreateResultDTO
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "fee must be a valid number"
                };
            }

            if (!Guid.TryParse(request.DoctorId, out var doctorId))
            {
                return new AppointmentCreateResultDTO
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "Invalid Doctor ID format"
                };
            }

            // Duplicate booking prevention
            var existingBooking = await _db.Appointments.FirstOrDefaultAsync(a =>
                a.DoctorId == doctorId &&
                a.CreatedBy == authenticatedUserId &&
                a.Date == request.Date &&
                a.Time == request.Time &&
                a.Status != AppointmentStatus.Canceled);

            if (existingBooking is not null)
            {
                return new AppointmentCreateResultDTO
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.Conflict,
                    ErrorMessage = "You already have an appointment with this doctor at the selected date and time."
                };
            }

            // Fetch doctor as source-of-truth
            backend_dotnet.Models.Domain.Doctor? doctor = null;
            try
            {
                doctor = await _db.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == doctorId);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Doctor lookup failed: {e.Message}");
            }

            if (doctor is null)
            {
                return new AppointmentCreateResultDTO
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessage = "Doctor not found"
                };
            }

            // Resolve owner, names, images, etc.
            var resolvedOwner = !string.IsNullOrWhiteSpace(request.Owner)
                ? request.Owner
                : (_majorAdminId ?? request.DoctorId);

            var doctorName = !string.IsNullOrWhiteSpace(doctor.User?.Name)
                ? doctor.User.Name.Trim()
                : (request.DoctorName?.Trim() ?? "");

            var speciality = !string.IsNullOrWhiteSpace(doctor.Specialization)
                ? doctor.Specialization.Trim()
                : (request.Speciality?.Trim() ?? "");

            var doctorImageUrl = !string.IsNullOrWhiteSpace(doctor.User?.ImageUrl)
                ? doctor.User.ImageUrl.Trim()
                : (request.DoctorImageUrl?.Trim() ?? "");

            var doctorImagePublicId = (request.DoctorImagePublicId?.Trim() ?? "");

            var appointment = new backend_dotnet.Models.Domain.Appointment
            {
                DoctorId = doctor.Id,
                DoctorName = doctorName,
                Speciality = speciality,
                DoctorImageUrl = doctorImageUrl,
                DoctorImagePubId = doctorImagePublicId,
                PatientName = request.PatientName.Trim(),
                Mobile = request.Mobile.Trim(),
                Age = int.TryParse(request.Age, out var parsedAge) ? parsedAge : null,
                Gender = request.Gender ?? "",
                Date = request.Date,
                Time = request.Time,
                Fees = numericFee.Value,
                CreatedBy = authenticatedUserId,
                Owner = resolvedOwner ?? "",
                SessionId = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Free appointment
            if (numericFee == 0)
            {
                appointment.Status = AppointmentStatus.Confirmed;
                appointment.PaymentMethod = string.Equals(request.PaymentMethod, "Cash", StringComparison.OrdinalIgnoreCase) ? PaymentMethod.Cash : PaymentMethod.Online;
                appointment.PaymentStatus = PaymentStatus.Paid;
                appointment.PaymentAmount = 0;
                appointment.PaidAt = DateTime.UtcNow;

                _db.Appointments.Add(appointment);
                await _db.SaveChangesAsync();

                return new AppointmentCreateResultDTO
                {
                    IsSuccess = true,
                    StatusCode = HttpStatusCode.Created,
                    Appointment = appointment,
                    CheckoutUrl = null
                };
            }

            // Cash payment
            if (request.PaymentMethod == "Cash")
            {
                appointment.Status = AppointmentStatus.Pending;
                appointment.PaymentMethod = PaymentMethod.Cash;
                appointment.PaymentStatus = PaymentStatus.Pending;
                appointment.PaymentAmount = numericFee.Value;

                _db.Appointments.Add(appointment);
                await _db.SaveChangesAsync();

                return new AppointmentCreateResultDTO
                {
                    IsSuccess = true,
                    StatusCode = HttpStatusCode.Created,
                    Appointment = appointment,
                    CheckoutUrl = null
                };
            }

            // Online: Razorpay
            if (string.IsNullOrWhiteSpace(_razorpayKeyId) || string.IsNullOrWhiteSpace(_razorpayKeySecret))
            {
                return new AppointmentCreateResultDTO
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
                appointment.PaymentAmount = numericFee.Value;

                _db.Appointments.Add(appointment);
                await _db.SaveChangesAsync();

                // Create Razorpay Order
                var client = new RazorpayClient(_razorpayKeyId, _razorpayKeySecret);
                var options = new Dictionary<string, object>
                {
                    { "amount", (int)(numericFee.Value * 100) }, // Amount in paise
                    { "currency", "INR" },
                    { "receipt", appointment.Id.ToString() }
                };
                Order order = client.Order.Create(options);
                string orderId = order["id"].ToString();

                appointment.SessionId = orderId;
                await _db.SaveChangesAsync();

                return new AppointmentCreateResultDTO
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
                return new AppointmentCreateResultDTO
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.InternalServerError,
                    ErrorMessage = "Failed to create appointment record or payment order"
                };
            }
        }

        private static decimal? SafeNumber(decimal? value)
        {
            if (value is null) return null;
            return value;
        }

        private string? BuildFrontendBase(string? origin)
        {
            if (!string.IsNullOrWhiteSpace(_frontendUrl))
                return _frontendUrl.TrimEnd('/');

            if (!string.IsNullOrWhiteSpace(origin))
                return origin.TrimEnd('/');

            return null;
        }
        //-------------------------------UpdateAppointment------------------------------------------------------
        public async Task<AppointmentUpdateResultDTO> UpdateAppointmentAsync(Guid id, UpdateAppointmentRequestDTO request)
        {
            var appt = await _db.Appointments.FindAsync(id);

            if (appt is null)
            {
                return new AppointmentUpdateResultDTO
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessage = "Appointment not found"
                };
            }

            var terminal = appt.Status == AppointmentStatus.Completed || appt.Status == AppointmentStatus.Canceled;

            if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<AppointmentStatus>(request.Status, true, out var parsedStatus))
            {
                if (terminal && parsedStatus != appt.Status)
                {
                    return new AppointmentUpdateResultDTO
                    {
                        IsSuccess = false,
                        StatusCode = HttpStatusCode.BadRequest,
                        ErrorMessage = "Cannot change status of a completed/canceled appointment"
                    };
                }
                appt.Status = parsedStatus;
            }

            if (!string.IsNullOrWhiteSpace(request.Date) && !string.IsNullOrWhiteSpace(request.Time))
            {
                if (appt.Status == AppointmentStatus.Completed || appt.Status == AppointmentStatus.Canceled)
                {
                    return new AppointmentUpdateResultDTO
                    {
                        IsSuccess = false,
                        StatusCode = HttpStatusCode.BadRequest,
                        ErrorMessage = "Cannot reschedule completed/canceled appointment"
                    };
                }

                appt.Date = request.Date;
                appt.Time = request.Time;
                appt.Status = AppointmentStatus.Rescheduled;
                appt.RescheduledDate = request.Date;
                appt.RescheduledTime = request.Time;
            }

            appt.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            var doctor = await _db.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == appt.DoctorId);

            var updatedProjection = new
            {
                appt.Id,
                appt.DoctorId,
                Doctor = doctor == null ? null : new
                {
                    doctor.User?.Name,
                    doctor.User?.ImageUrl
                },
                appt.PatientName,
                appt.Mobile,
                appt.Age,
                appt.Gender,
                appt.Date,
                appt.Time,
                appt.Fees,
                appt.Status,
                appt.RescheduledDate,
                appt.RescheduledTime,
                appt.CreatedBy,
                appt.Owner,
                appt.CreatedAt,
                appt.UpdatedAt
            };

            return new AppointmentUpdateResultDTO
            {
                IsSuccess = true,
                Appointment = updatedProjection
            };
        }
        //-------------------------------CancelAppointment------------------------------------------------------
        public async Task<AppointmentUpdateResultDTO> CancelAppointmentAsync(Guid id)
        {
            var appt = await _db.Appointments.FindAsync(id);

            if (appt is null)
            {
                return new AppointmentUpdateResultDTO
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessage = "Appointment not found"
                };
            }

            appt.Status = AppointmentStatus.Canceled;
            appt.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return new AppointmentUpdateResultDTO
            {
                IsSuccess = true,
                Appointment = appt
            };
        }

        //-------------------------------GetStatsAsync------------------------------------------------------
        public async Task<AppointmentStatsResultDTO> GetStatsAsync()
        {
            var total = await _db.Appointments.CountAsync();

            var revenue = await _db.Appointments
                .Where(a => a.PaymentStatus == PaymentStatus.Paid)
                .SumAsync(a => (decimal?)a.Fees) ?? 0;

            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

            var recent = await _db.Appointments
                .Where(a => a.CreatedAt >= sevenDaysAgo)
                .CountAsync();

            return new AppointmentStatsResultDTO
            {
                IsSuccess = true,
                Total = total,
                Revenue = revenue,
                RecentLast7Days = recent
            };
        }

        //-------------------------------GetAppointmentsByDoctor------------------------------------------------------
        public async Task<AppointmentListResultDTO> GetAppointmentsByDoctorAsync(Guid doctorId, GetAppointmentsByDoctorQueryDTO query)
        {

            var limit = Math.Min(200, Math.Max(1, query.Limit ?? 50));
            var page = Math.Max(1, query.Page ?? 1);
            var skip = (page - 1) * limit;

            // The frontend passes user.id (UserId) in the URL. If this is a UserId, resolve it to the DoctorId.
            var actualDoctorId = doctorId;
            var doctorByUserId = await _db.Doctors.AsNoTracking().FirstOrDefaultAsync(d => d.UserId == doctorId);
            if (doctorByUserId != null)
            {
                actualDoctorId = doctorByUserId.Id;
            }

            var appointmentsQuery = _db.Appointments.AsNoTracking()
                .Where(a => a.IsActive && !a.IsDeleted && a.Doctor.IsActive && !a.Doctor.IsDeleted)
                .Where(a => a.DoctorId == actualDoctorId);

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

            var items = await appointmentsQuery
                .OrderBy(a => a.Date)
                .ThenBy(a => a.Time)
                .Skip(skip)
                .Take(limit)
                .ToListAsync();

            // manual "populate" of doctor summary fields (see FK note below)
            var doctor = await _db.Doctors.Include(d => d.User).AsNoTracking().FirstOrDefaultAsync(d => d.Id == doctorId);

            var projected = items.Select(a => (object)new
            {
                a.Id,
                a.DoctorId,
                Doctor = doctor == null ? null : new
                {
                    doctor.User?.Name,
                    doctor.Specialization,
                    doctor.User?.ImageUrl
                },
                a.Mobile,
                a.Status,
                a.PatientName,
                a.Date,
                a.Time,
                a.Fees,
                a.CreatedBy,
                a.CreatedAt
            }).ToList();

            return new AppointmentListResultDTO
            {
                IsSuccess = true,
                Appointments = projected,
                Page = page,
                Limit = limit,
                Total = total,
                Count = projected.Count
            };
        }

        //-------------------------------GetAppointmentsByPatient------------------------------------------------------

        //-------------------------------GetAppointmentsByPatient------------------------------------------------------








        //-------------------------------VerifyRazorpayPayment------------------------------------------------------
        public async Task<AppointmentCreateResultDTO> VerifyRazorpayPaymentAsync(string razorpayOrderId, string razorpayPaymentId, string razorpaySignature)
        {
            if (string.IsNullOrWhiteSpace(razorpayOrderId) || string.IsNullOrWhiteSpace(razorpayPaymentId) || string.IsNullOrWhiteSpace(razorpaySignature))
            {
                return new AppointmentCreateResultDTO
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
                return new AppointmentCreateResultDTO
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "Invalid payment signature"
                };
            }

            var appt = await _db.Appointments.FirstOrDefaultAsync(a => a.SessionId == razorpayOrderId);
            if (appt is null)
            {
                return new AppointmentCreateResultDTO
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessage = "Appointment not found"
                };
            }

            if (appt.PaymentStatus != PaymentStatus.Paid)
            {
                appt.PaymentStatus = PaymentStatus.Paid;
                appt.PaymentProviderId = razorpayPaymentId;
                appt.Status = AppointmentStatus.Confirmed;
                appt.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();
            }

            return new AppointmentCreateResultDTO
            {
                IsSuccess = true,
                Appointment = appt
            };
        }
    }
}
