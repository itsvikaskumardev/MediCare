namespace backend_dotnet.Models.DTOs.Appointment
{
    public class CreateAppointmentRequestDTO
    {
        public string DoctorId { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string? Age { get; set; }
        public string? Gender { get; set; }
        public string Date { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public decimal? Fee { get; set; }
        public decimal? Fees { get; set; }
        public string? Notes { get; set; }
        public string? Email { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Owner { get; set; }
        public string? DoctorName { get; set; }
        public string? Speciality { get; set; }
        public string? DoctorImageUrl { get; set; }
        public string? DoctorImagePublicId { get; set; }
    }
}
