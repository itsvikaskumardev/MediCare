namespace backend_dotnet.Models.DTOs.ServiceAppointment
{
    public class GetServiceAppointmentsQueryDTO
    {
        public string? ServiceId { get; set; }
        public string? Mobile { get; set; }
        public string? Status { get; set; }
        public int? Page { get; set; } = 1;
        public int? Limit { get; set; } = 50;
        public string? Search { get; set; } = "";
    }
}
