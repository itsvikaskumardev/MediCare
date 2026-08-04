using System.Net;

namespace backend_dotnet.Models.DTOs.Appointment
{
    public class AppointmentUpdateResultDTO
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        public object? Appointment { get; set; }
    }
}
