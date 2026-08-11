using System.Net;

namespace backend_dotnet.Models.DTOs.ServiceAppointment
{
    public class ServiceAppointmentCreateResultDTO
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        public object? Appointment { get; set; }
        public string? CheckoutUrl { get; set; }
        public string? RazorpayOrderId { get; set; }
        public string? RazorpayKeyId { get; set; }
    }
}
