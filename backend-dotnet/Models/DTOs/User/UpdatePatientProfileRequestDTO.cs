using System.Net;

namespace backend_dotnet.Models.DTOs.User
{
    public class UpdatePatientProfileRequestDTO
    {
        public string? Name { get; set; }
        public string? Mobile { get; set; }
        public int? Age { get; set; }
        public string? Gender { get; set; }
        
        // From Patient model
        public string? BloodGroup { get; set; }
        public string? MedicalHistory { get; set; }
        public string? Allergies { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactNumber { get; set; }
        public string? InsuranceProvider { get; set; }
        public string? InsurancePolicyNumber { get; set; }
    }

    public class UpdatePatientProfileResultDTO
    {
        public bool IsSuccess { get; set; } = true;
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        public string? ErrorMessage { get; set; }
        public PatientProfileDTO? Profile { get; set; }
    }
}
