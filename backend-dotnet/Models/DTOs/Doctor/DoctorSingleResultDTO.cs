namespace backend_dotnet.Models.DTOs.Doctor
{
    public class DoctorSingleResultDTO
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public DoctorResponseDTO? Data { get; set; }
    }
}
