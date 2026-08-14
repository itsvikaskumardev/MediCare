namespace backend_dotnet.Models.DTOs.Doctor
{
    public class UpdateDoctorRequestDTO
    {

        public string? Name { get; set; }
        public string? Specialization { get; set; }
        public string? Experience { get; set; }
        public string? Qualifications { get; set; }
        public string? Location { get; set; }
        public string? About { get; set; }
        public decimal? Fee { get; set; }
        public string? Availability { get; set; }
        public string? Success { get; set; }
        public string? Patients { get; set; }
        public double? Rating { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? ImageUrl { get; set; }
        public string? Schedule { get; set; }
        public Microsoft.AspNetCore.Http.IFormFile? Image { get; set; }
    }
}
