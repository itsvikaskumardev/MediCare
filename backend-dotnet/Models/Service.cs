using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_dotnet.Models
{
    public class Service
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        public string? About { get; set; }

        public string? ShortDescription { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; } = 0;

        public bool Available { get; set; } = true;

        public string? ImageUrl { get; set; }

        public string? ImagePublicId { get; set; }

        // Native PostgreSQL text[] array
        [Column(TypeName = "text[]")]
        public string[] Dates { get; set; } = Array.Empty<string>();

        // PostgreSQL jsonb column
        [Column(TypeName = "jsonb")]
        public string? Slots { get; set; } = "{}";

        // Native PostgreSQL text[] array
        [Column(TypeName = "text[]")]
        public string[] Instructions { get; set; } = Array.Empty<string>();

        public int TotalAppointments { get; set; } = 0;

        public int Completed { get; set; } = 0;

        public int Canceled { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<ServiceAppointment> ServiceAppointments { get; set; } = new List<ServiceAppointment>();
    }
}
