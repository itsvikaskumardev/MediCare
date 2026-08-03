namespace backend_dotnet.Models.DTOs.Appointment
{
    public class AppointmentListByPatientResultDTO
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public bool IsAuthError { get; set; }
        public List<object>? Appointments { get; set; }
    }
}
