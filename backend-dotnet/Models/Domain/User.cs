using System.ComponentModel.DataAnnotations;

namespace backend_dotnet.Models.Domain
{
    public class User
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Optional: populated when syncing from Clerk
        [MaxLength(255)]
        public string? ClerkId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        // Optional: only required for custom email/password auth
        public string? PasswordHash { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Mobile { get; set; }

        public int? Age { get; set; }

        [MaxLength(20)]
        public string? Gender { get; set; }

        public Role Role { get; set; } = Role.PATIENT;

        public string? ImageUrl { get; set; }

        public string? ImagePublicId { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public ICollection<ServiceAppointment> ServiceAppointments { get; set; } = new List<ServiceAppointment>();
        public Doctor? DoctorProfile { get; set; }
        public Patient? PatientProfile { get; set; }
    }
}
