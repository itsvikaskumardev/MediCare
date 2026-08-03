using backend_dotnet.Models.DTOs.Appointment;

namespace backend_dotnet.Services.Appointment
{
    public interface IAppointmentService
    {
        // Service methods for Appointment module will be defined here
        Task<AppointmentListResultDTO> GetAppointmentsAsync(GetAppointmentsQueryDTO query);

    }
}
