using backend_dotnet.Data;

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
    }
}
