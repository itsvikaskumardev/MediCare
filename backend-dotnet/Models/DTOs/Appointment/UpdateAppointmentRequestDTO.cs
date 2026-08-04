namespace backend_dotnet.Models.DTOs.Appointment
{
    public class UpdateAppointmentRequestDTO
    {
        public string? Status { get; set; }
        public string? Notes { get; set; }
        public string? Date { get; set; }
        public string? Time { get; set; }
    }
}
