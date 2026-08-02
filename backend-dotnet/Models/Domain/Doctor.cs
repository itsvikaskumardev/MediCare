using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_dotnet.Models.Domain
{
    public class Doctor
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        public string? Specialization { get; set; }

        public string? ImageUrl { get; set; }

        public string? ImagePublicId { get; set; }

        public string? Experience { get; set; }

        public string? Qualifications { get; set; }

        public string? Location { get; set; }

        public string? About { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Fee { get; set; } = 0;

        public Availability Availability { get; set; } = Availability.Available;

        // PostgreSQL jsonb column
        [Column(TypeName = "jsonb")]
        public string? Schedule { get; set; } = "{}";

        public string? Success { get; set; }

        public string? Patients { get; set; }

        [Column(TypeName = "decimal(3,2)")]
        public decimal Rating { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
