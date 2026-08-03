namespace backend_dotnet.Models.DTOs.Appointment
{
    public class GetAppointmentsByPatientQueryDTO
    {
        public string? CreatedBy { get; set; }
        public string? Mobile { get; set; }
    }
}
