using backend_dotnet.Data;
using backend_dotnet.Models.DTOs.Appointment;
using backend_dotnet.Models.DTOs.ServiceAppointment;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.RegularExpressions;

namespace backend_dotnet.Services.ServiceAppointment
{
    public class ServiceAppointmentService : IServiceAppointmentService
    {
        private readonly ApplicationDbContext _db;

        public ServiceAppointmentService(ApplicationDbContext db)
        {
            _db = db;
        }

        //--------------------------------GetServiceAppointmentsAsync--------------------------------------------------------

        public async Task<ServiceAppointmentListResultDTO> GetServiceAppointmentsAsync(GetServiceAppointmentsQueryDTO query)
        {
            var limit = Math.Min(200, Math.Max(1, query.Limit ?? 50));
            var page = Math.Max(1, query.Page ?? 1);
            var skip = (page - 1) * limit;

            var appointmentsQuery = _db.ServiceAppointments.AsQueryable();

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

            var items = await appointmentsQuery
                .OrderByDescending(a => a.CreatedAt)
                .Skip(skip)
                .Take(limit)
                .ToListAsync();

            // manual "populate" of service summary fields
            var serviceIds = items.Select(a => a.ServiceId).Distinct().ToList();

            // Step 1: Fetch from DB into memory (async)
            var services = await _db.Services
                .Where(s => serviceIds.Contains(s.Id))
                .ToListAsync();

            var projected = items.Select(a =>
            {
                // Step 2: Search the in-memory list (sync)
                var svc = services.FirstOrDefault(s => s.Id == a.ServiceId);

                return (object)new
                {
                    a.Id,
                    a.ServiceId,
                    Service = svc == null ? null : new
                    {
                        svc.Name,
                        svc.ImageUrl
                    },
                    a.Mobile,
                    a.Status,
                    a.PatientName,
                    a.CreatedAt
                };
            }).ToList();

            return new ServiceAppointmentListResultDTO
            {
                IsSuccess = true,
                Appointments = projected,
                Page = page,
                Limit = limit,
                Total = total,
                Count = projected.Count
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
                Data = existing
            };
        }
        //--------------------------------CancelServiceAppointmentAsync--------------------------------------------------------

        public async Task<ServiceAppointmentCancelResultDTO> CancelServiceAppointmentAsync(Guid id)
        {
            var appt = await _db.ServiceAppointments.FindAsync(id);

            if (appt is null)
            {
                return new ServiceAppointmentCancelResultDTO
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessage = "Not found"
                };
            }

            if (appt.Status == AppointmentStatus.Completed)
            {
                return new ServiceAppointmentCancelResultDTO
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "Cannot cancel a completed appointment"
                };
            }

            appt.Status = AppointmentStatus.Canceled;

            if (appt.PaymentStatus == PaymentStatus.Paid)
            {
                appt.PaymentStatus = PaymentStatus.Refunded;
            }
            else
            {
                appt.PaymentStatus = PaymentStatus.Pending;
            }

            appt.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return new ServiceAppointmentCancelResultDTO
            {
                IsSuccess = true,
                Data = appt
            };
        }

        //--------------------------------GetServiceAppointmentStatsAsync--------------------------------------------------------

        public async Task<ServiceAppointmentStatsResultDTO> GetServiceAppointmentStatsAsync()
        {
            var services = await _db.Services.AsNoTracking().ToListAsync();
            var allAppointments = await _db.ServiceAppointments.AsNoTracking().ToListAsync();

            var resultList = services.Select(s =>
            {
                var appts = allAppointments.Where(a => a.ServiceId == s.Id).ToList();
                var totalAppointments = appts.Count;
                var completed = appts.Count(a => a.Status == AppointmentStatus.Completed);
                var canceled = appts.Count(a => a.Status == AppointmentStatus.Canceled);
                var earning = completed * s.Price;

                return (object)new
                {
                    name = s.Name,
                    price = s.Price,
                    image = s.ImageUrl,
                    totalAppointments,
                    completed,
                    canceled,
                    earning
                };
            }).ToList();

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

            var appointmentsQuery = _db.ServiceAppointments.AsQueryable();

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
    }
}
