using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
namespace backend_dotnet.Models.DTOs.Doctor
{
    public class CreateDoctorRequestDTO
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Specialization { get; set; }
        public string? Experience { get; set; }
        public string? Qualifications { get; set; }
        public string? Location { get; set; }
        public string? About { get; set; }
        public decimal? Fee { get; set; }
        public string? Availability { get; set; }
        public string? Schedule { get; set; }
        public string? Success { get; set; }
        public string? Patients { get; set; }
        public decimal? Rating { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImagePublicId { get; set; }

        [FromForm(Name = "image")]
        public IFormFile? Image { get; set; }
    }
}
