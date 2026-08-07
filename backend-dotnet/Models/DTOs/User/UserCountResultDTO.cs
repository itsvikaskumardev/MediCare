namespace backend_dotnet.Models.DTOs.User
{
    public class UserCountResultDTO
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public int TotalUsers { get; set; }
        public int TotalPatients { get; set; }
        public int TotalAdmins { get; set; }
    }
}
