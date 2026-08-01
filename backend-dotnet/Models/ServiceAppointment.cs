using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_dotnet.Models
{
    public class ServiceAppointment
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Optional relation to local User table
        public Guid? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [MaxLength(255)]
        public string? CreatedBy { get; set; } // Clerk / external auth ID

        [Required]
        [MaxLength(255)]
        public string PatientName { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Mobile { get; set; } = string.Empty;

        public int? Age { get; set; }

        [MaxLength(20)]
        public string? Gender { get; set; }

        [Required]
        public Guid ServiceId { get; set; }

        [ForeignKey(nameof(ServiceId))]
        public Service Service { get; set; } = null!;

        // Denormalized fields
        [Required]
        [MaxLength(255)]
        public string ServiceName { get; set; } = string.Empty;

        public string? ServiceImageUrl { get; set; }

        public string? ServiceImagePubId { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Fees { get; set; }

        [Required]
        [MaxLength(10)] // YYYY-MM-DD
        public string Date { get; set; } = string.Empty;

        [Required]
        public int Hour { get; set; }

        [Required]
        public int Minute { get; set; }

        [Required]
        [MaxLength(2)] // AM / PM
        public string Ampm { get; set; } = string.Empty;

        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

        public string? RescheduledDate { get; set; }

        public int? RescheduledHour { get; set; }

        public int? RescheduledMinute { get; set; }

        public string? RescheduledAmpm { get; set; }

        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        [Column(TypeName = "decimal(10,2)")]
        public decimal PaymentAmount { get; set; }

        public string? PaymentProviderId { get; set; }

        public string? PaymentSessionId { get; set; }

        // PostgreSQL jsonb column
        [Column(TypeName = "jsonb")]
        public string? PaymentMeta { get; set; }

        public DateTime? PaidAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
