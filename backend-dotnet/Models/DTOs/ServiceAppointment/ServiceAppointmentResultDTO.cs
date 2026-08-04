namespace backend_dotnet.Models.DTOs.ServiceAppointment
{
    public class ServiceAppointmentResultDTO
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public object? Data { get; set; }
    }
}
