using System.ComponentModel.DataAnnotations;

namespace backend_dotnet.Models.DTOs.Auth
{
    public class PatientRegistrationRequestDto
    {
        [Required]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public string Password { get; set; } = string.Empty;

        public string? Mobile { get; set; }
        public int? Age { get; set; }
        public string? Gender { get; set; }

        public string? BloodGroup { get; set; }
        public string? MedicalHistory { get; set; }
        public string? Allergies { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactNumber { get; set; }
        public string? InsuranceProvider { get; set; }
        public string? InsurancePolicyNumber { get; set; }
    }
}
