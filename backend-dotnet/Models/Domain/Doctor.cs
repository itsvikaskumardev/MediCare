using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_dotnet.Models.Domain
{
    public class Doctor
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey(nameof(Id))]
        public User User { get; set; } = null!;

        public string? Specialization { get; set; }

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

        // Navigation properties
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
