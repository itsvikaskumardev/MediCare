using System.ComponentModel.DataAnnotations;

namespace backend_dotnet.Models.DTOs
{
    public class LoginResponseDTO
    {
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;

        public string Role { get; set; } = "PATIENT";

    }
}
