using backend_dotnet.Data;
using backend_dotnet.Models.DTOs.Appointment;
using Microsoft.EntityFrameworkCore;

namespace backend_dotnet.Services.ServiceAppointment
{
    public class ServiceAppointmentService : IServiceAppointmentService
    {
        private readonly ApplicationDbContext _db;

        public ServiceAppointmentService(ApplicationDbContext db)
        {
            _db = db;
        }

      

        // Implementation of ServiceAppointment module methods will be defined here
    }
}
