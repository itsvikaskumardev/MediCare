namespace backend_dotnet.Models.DTOs.Appointment
{
    public class AppointmentStatsResultDTO
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public int Total { get; set; }
        public decimal Revenue { get; set; }
        public int RecentLast7Days { get; set; }
    }
}
