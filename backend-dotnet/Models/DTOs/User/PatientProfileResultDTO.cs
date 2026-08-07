using System.Net;

namespace backend_dotnet.Models.DTOs.User
{
    public class PatientProfileResultDTO
    {
        public bool IsSuccess { get; set; } = true;
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        public string? ErrorMessage { get; set; }
        public PatientProfileDTO? Profile { get; set; }
    }

    public class PatientProfileDTO
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Mobile { get; set; }
        public int? Age { get; set; }
        public string? Gender { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImagePublicId { get; set; }

        // Patient specific
        public string? BloodGroup { get; set; }
        public string? MedicalHistory { get; set; }
        public string? Allergies { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactNumber { get; set; }
        public string? InsuranceProvider { get; set; }
        public string? InsurancePolicyNumber { get; set; }
    }
}
