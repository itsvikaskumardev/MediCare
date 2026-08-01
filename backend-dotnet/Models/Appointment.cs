using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_dotnet.Models
{
    public class Appointment
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Optional relation to local User table
        public Guid? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        // Clerk / external auth ID (backward compatibility)
        [Required]
        [MaxLength(255)]
        public string Owner { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? CreatedBy { get; set; }

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
        public Guid DoctorId { get; set; }

        [ForeignKey(nameof(DoctorId))]
        public Doctor Doctor { get; set; } = null!;

        // Denormalized fields for quick UI access
        public string? DoctorName { get; set; }
        public string? Speciality { get; set; }
        public string? DoctorImageUrl { get; set; }
        public string? DoctorImagePubId { get; set; }

        [Required]
        [MaxLength(10)] // YYYY-MM-DD
        public string Date { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string Time { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal Fees { get; set; }

        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

        public string? RescheduledDate { get; set; }

        public string? RescheduledTime { get; set; }

        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        [Column(TypeName = "decimal(10,2)")]
        public decimal PaymentAmount { get; set; } = 0;

        public string? PaymentProviderId { get; set; }

        // PostgreSQL jsonb column
        [Column(TypeName = "jsonb")]
        public string? PaymentMeta { get; set; }

        public string? SessionId { get; set; }

        public DateTime? PaidAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
