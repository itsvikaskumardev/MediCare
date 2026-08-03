namespace backend_dotnet.Models.DTOs.Appointment
{
    public class AppointmentListResultDTO
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public List<object>? Appointments { get; set; }
        public int Page { get; set; }
        public int Limit { get; set; }
        public int Total { get; set; }
        public int Count { get; set; }
    }
}
