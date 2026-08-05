using System.ComponentModel.DataAnnotations;

namespace backend_dotnet.Models.DTOs.Auth
{
    public class AdminRegistrationRequestDto
    {
        [Required]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
