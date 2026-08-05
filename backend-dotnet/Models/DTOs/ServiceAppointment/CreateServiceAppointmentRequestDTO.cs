namespace backend_dotnet.Models.DTOs.ServiceAppointment
{
    public class CreateServiceAppointmentRequestDTO
    {
        public string ServiceId { get; set; } = string.Empty;
        public string? ServiceName { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string? Age { get; set; }
        public string? Gender { get; set; }
        public string Date { get; set; } = string.Empty;
        public string? Time { get; set; }
        public int? Hour { get; set; }
        public int? Minute { get; set; }
        public string? AmPm { get; set; }
        public string PaymentMethod { get; set; } = "Online";
        public decimal? Amount { get; set; }
        public decimal? Fees { get; set; }
        public string? Email { get; set; }
        public Dictionary<string, object>? Meta { get; set; }
        public string? Notes { get; set; }
        public string? ServiceImageUrl { get; set; }
        public string? ServiceImagePublicId { get; set; }
    }
}
