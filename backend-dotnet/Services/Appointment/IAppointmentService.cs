using backend_dotnet.Models.DTOs.Appointment;

namespace backend_dotnet.Services.Appointment
{
    public interface IAppointmentService
    {
        // Service methods for Appointment module will be defined here
        Task<AppointmentListResultDTO> GetAppointmentsAsync(GetAppointmentsQueryDTO query);
        Task<AppointmentResultDTO> GetAppointmentByIdAsync(Guid id);
        Task<AppointmentListByPatientResultDTO> GetAppointmentsByPatientAsync(GetAppointmentsByPatientQueryDTO query, string? authenticatedUserId);
        Task<AppointmentCreateResultDTO> CreateAppointmentAsync(CreateAppointmentRequestDTO request, string? authenticatedUserId, string? frontendOrigin);
    }

}
