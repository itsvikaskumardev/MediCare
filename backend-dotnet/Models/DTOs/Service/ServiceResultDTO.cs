namespace backend_dotnet.Models.DTOs.Service
{
    public class ServiceResultDTO
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public object? Data { get; set; }
    }
}
