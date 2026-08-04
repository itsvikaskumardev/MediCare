namespace backend_dotnet.Models.DTOs.User
{
    public class UserCountResultDTO
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public int TotalUsers { get; set; }
    }
}
