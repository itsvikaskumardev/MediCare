namespace backend_dotnet.Models.DTOs.Appointment
{
    public class AppointmentResultDTO
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public object? Appointment { get; set; }
    }
}
