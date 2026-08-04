using backend_dotnet.Models.DTOs.Appointment;
using backend_dotnet.Models.DTOs.ServiceAppointment;

namespace backend_dotnet.Services.ServiceAppointment
{
    public interface IServiceAppointmentService
    {
        // Service methods for ServiceAppointment module will be defined here
        Task<ServiceAppointmentListResultDTO> GetServiceAppointmentsAsync(GetServiceAppointmentsQueryDTO query);
        Task<ServiceAppointmentResultDTO> GetServiceAppointmentByIdAsync(Guid id);

        Task<ServiceAppointmentUpdateResultDTO> UpdateServiceAppointmentAsync(Guid id, UpdateServiceAppointmentRequestDTO request);
        Task<ServiceAppointmentCancelResultDTO> CancelServiceAppointmentAsync(Guid id);
        Task<ServiceAppointmentStatsResultDTO> GetServiceAppointmentStatsAsync();

        Task<ServiceAppointmentListByPatientResultDTO> GetServiceAppointmentsByPatientAsync(GetServiceAppointmentsByPatientQueryDTO query, string? authenticatedUserId);
    }
}
