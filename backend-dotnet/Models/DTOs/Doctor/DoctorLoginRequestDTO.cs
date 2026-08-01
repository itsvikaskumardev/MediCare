using System.ComponentModel.DataAnnotations;

namespace backend_dotnet.Models.DTOs.Doctor
{
    public class DoctorLoginRequestDTO
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
