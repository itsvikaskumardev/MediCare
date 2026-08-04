namespace backend_dotnet.Models.DTOs.ServiceAppointment
{
    public class ServiceAppointmentListByPatientResultDTO
    {
        public bool IsSuccess { get; set; }
        public List<object>? Data { get; set; }
    }
}
