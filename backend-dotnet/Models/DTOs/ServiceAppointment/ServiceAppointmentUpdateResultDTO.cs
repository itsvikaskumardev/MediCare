using System.Net;

namespace backend_dotnet.Models.DTOs.ServiceAppointment
{
    public class ServiceAppointmentUpdateResultDTO
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        public object? Data { get; set; }
    }
}
