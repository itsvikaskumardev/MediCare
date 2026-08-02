namespace backend_dotnet.Models.DTOs.Doctor
{
    public class DoctorToggleAvailabilityResultDTO
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public DoctorResponseDTO? Data { get; set; }
    }
}
