namespace backend_dotnet.Models.DTOs.Appointment
{
    public class GetAppointmentsQueryDTO
    {
        public string? DoctorId { get; set; }
        public string? Mobile { get; set; }
        public string? Status { get; set; }
        public string? Search { get; set; } = "";
        public int? Limit { get; set; } = 50;
        public int? Page { get; set; } = 1;
        public string? PatientClerkId { get; set; }
        public string? CreatedBy { get; set; }
    }
}
