namespace backend_dotnet.Models.DTOs.Doctor
{
    public class DoctorAuthResultDTO
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Token { get; set; }
        public object? Data { get; set; }
    }
}
