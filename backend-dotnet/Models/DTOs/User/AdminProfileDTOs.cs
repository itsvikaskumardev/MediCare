using System.Net;

namespace backend_dotnet.Models.DTOs.User
{
    public class AdminProfileDTO
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public Guid Id { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class AdminProfileResultDTO
    {
        public bool IsSuccess { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string? ErrorMessage { get; set; }
        public AdminProfileDTO? Profile { get; set; }
    }

    public class UpdateAdminProfileRequestDTO
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }
}
