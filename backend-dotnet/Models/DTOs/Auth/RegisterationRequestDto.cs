using System.ComponentModel.DataAnnotations;

namespace backend_dotnet.Models.DTOs
{
    public class RegisterationRequestDto
    {

        [Required]
        public string Email { get; set; } = string.Empty;
        [Required]

        public string Name { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
        [Required]
        public string Role { get; set; } = "PATIENT";
    }
}
