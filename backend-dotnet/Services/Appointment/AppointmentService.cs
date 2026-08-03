using backend_dotnet.Data;
using backend_dotnet.Models.DTOs.Appointment;
using Microsoft.EntityFrameworkCore;

namespace backend_dotnet.Services.Appointment
{
    public class AppointmentService : IAppointmentService
    {
        private readonly ApplicationDbContext _db;

        public AppointmentService(ApplicationDbContext db)
        {
            _db = db;
        }

        // Implementation of Appointment service methods will be defined here

        public async Task<AppointmentListResultDTO> GetAppointmentsAsync(GetAppointmentsQueryDTO query)
        {
            var limit = Math.Min(200, Math.Max(1, query.Limit ?? 50));
            var page = Math.Max(1, query.Page ?? 1);
            var skip = (page - 1) * limit;

            var appointmentsQuery = _db.Appointments.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.DoctorId))
                appointmentsQuery = appointmentsQuery.Where(a => a.DoctorId == query.DoctorId);

            if (!string.IsNullOrWhiteSpace(query.Mobile))
                appointmentsQuery = appointmentsQuery.Where(a => a.Mobile == query.Mobile);

            if (!string.IsNullOrWhiteSpace(query.Status))
                appointmentsQuery = appointmentsQuery.Where(a => a.Status == query.Status);

            if (!string.IsNullOrWhiteSpace(query.PatientClerkId))
                appointmentsQuery = appointmentsQuery.Where(a => a.CreatedBy == query.PatientClerkId);

            if (!string.IsNullOrWhiteSpace(query.CreatedBy))
                appointmentsQuery = appointmentsQuery.Where(a => a.CreatedBy == query.CreatedBy);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search;
                appointmentsQuery = appointmentsQuery.Where(a =>
                    EF.Functions.ILike(a.PatientName, $"%{search}%") ||
                    EF.Functions.ILike(a.Mobile, $"%{search}%") ||
                    EF.Functions.ILike(a.Notes, $"%{search}%"));
            }

            var total = await appointmentsQuery.CountAsync();

            var items = await appointmentsQuery
                .OrderByDescending(a => a.CreatedAt)
                .Skip(skip)
                .Take(limit)
                .Include(a => a.Doctor)
                .Select(a => new
                {
                    a.Id,
                    a.DoctorId,
                    Doctor = a.Doctor == null ? null : new
                    {
                        a.Doctor.Name,
                        a.Doctor.Specialization,
                        a.Doctor.ImageUrl
                        // add Owner / Image here if those fields exist on your Doctor model
                    },
                    a.Mobile,
                    a.Status,
                    a.PatientName,
                    a.Notes,
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
    }
}
