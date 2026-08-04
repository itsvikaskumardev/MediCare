namespace backend_dotnet.Models.DTOs.ServiceAppointment
{
    public class ServiceAppointmentStatsResultDTO
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public List<object>? Services { get; set; }
        public int TotalServices { get; set; }
    }
}
