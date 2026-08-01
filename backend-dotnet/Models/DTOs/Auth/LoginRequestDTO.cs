using System.ComponentModel.DataAnnotations;

namespace backend_dotnet.Models.DTOs
{
    public class LoginRequestDTO
    {


        [Required]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
