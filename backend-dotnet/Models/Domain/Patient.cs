using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_dotnet.Models.Domain
{
    public class Patient
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid UserId { get; set; }
        
        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        public string? BloodGroup { get; set; }

        public string? MedicalHistory { get; set; }

        public string? Allergies { get; set; }

        public string? EmergencyContactName { get; set; }

        public string? EmergencyContactNumber { get; set; }

        public string? InsuranceProvider { get; set; }

        public string? InsurancePolicyNumber { get; set; }
    }
}
