using backend_dotnet.Models;
using backend_dotnet.Models.Domain;

namespace backend_dotnet.Models.DTOs.Doctor
{
    public class DoctorResponseDTO
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Specialization { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImagePublicId { get; set; }
        public Availability Availability { get; set; }
        public string? Experience { get; set; }
        public string? Qualifications { get; set; }
        public string? Location { get; set; }
        public string? About { get; set; }
        public decimal Fee { get; set; }
        public string? Schedule { get; set; }
        public string? Success { get; set; }
        public string? Patients { get; set; }
        public decimal Rating { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public DoctorResponseDTO() { }

        public DoctorResponseDTO(backend_dotnet.Models.Domain.Doctor doctor)
        {
            Id = doctor.Id;
            Email = doctor.User?.Email ?? string.Empty;
            Name = doctor.User?.Name ?? string.Empty;
            Specialization = doctor.Specialization;
            ImageUrl = doctor.User?.ImageUrl;
            ImagePublicId = null;
            Availability = doctor.Availability;
            Experience = doctor.Experience;
            Qualifications = doctor.Qualifications;
            Location = doctor.Location;
            About = doctor.About;
            Fee = doctor.Fee;
            Schedule = doctor.Schedule;
            Success = doctor.Success;
            Patients = doctor.Patients;
            Rating = doctor.Rating;
            CreatedAt = doctor.User?.CreatedAt ?? DateTime.UtcNow;
            UpdatedAt = doctor.User?.UpdatedAt ?? DateTime.UtcNow;
        }
    }
}
