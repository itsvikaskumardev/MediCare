using backend_dotnet.Data;
using backend_dotnet.Models.DTOs.Doctor;
using backend_dotnet.Services.ImageUpload;
using backend_dotnet.Services.Jwt;
using backend_dotnet.Services.Password;
using Microsoft.EntityFrameworkCore;

namespace backend_dotnet.Services.Doctor
{
    public class DoctorService : IDoctorService
    {
        private readonly ApplicationDbContext _db;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IImageUploadService _imageUploadService;

        public DoctorService(
            ApplicationDbContext db,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator,
            IImageUploadService imageUploadService)
        {
            _db = db;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
            _imageUploadService = imageUploadService;
        }

        public async Task<DoctorAuthResultDTO> CreateDoctorAsync(CreateDoctorRequestDTO request, IFormFile? image)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.Name))
            {
                return new DoctorAuthResultDTO
                {
                    IsSuccess = false,
                    ErrorMessage = "Email, Password and Name are required"
                };
            }

            var emailLc = request.Email.Trim().ToLowerInvariant();

            if (await _db.Doctors.AnyAsync(d => d.Email.ToLower() == emailLc))
            {
                return new DoctorAuthResultDTO
                {
                    IsSuccess = false,
                    ErrorMessage = "A doctor with this email already exists"
                };
            }

            string? imageUrl = request.ImageUrl;
            string? imagePublicId = request.ImagePublicId;

            if (image is not null && image.Length > 0)
            {
                var uploadedUrl = await _imageUploadService.UploadImageAsync(image, "doctors");
                if (!string.IsNullOrEmpty(uploadedUrl))
                {
                    imageUrl = uploadedUrl;
                }
            }

            var availability = Enum.TryParse<Availability>(request.Availability, true, out var parsedAvailability)
                ? parsedAvailability
                : Availability.Available;

            var passwordHash = _passwordHasher.HashPassword(request.Password);

            var doctor = new backend_dotnet.Models.Doctor
            {
                Email = emailLc,
                Password = passwordHash,
                Name = request.Name,
                Specialization = request.Specialization ?? "",
                ImageUrl = imageUrl,
                ImagePublicId = imagePublicId,
                Availability = availability,
                Experience = request.Experience ?? "",
                Qualifications = request.Qualifications ?? "",
                Location = request.Location ?? "",
                About = request.About ?? "",
                Fee = request.Fee ?? 0,
                Schedule = string.IsNullOrWhiteSpace(request.Schedule) ? "{}" : request.Schedule,
                Success = request.Success ?? "",
                Patients = request.Patients ?? "",
                Rating = request.Rating ?? 0
            };

            _db.Doctors.Add(doctor);
            await _db.SaveChangesAsync();

            var token = _jwtTokenGenerator.GenerateToken(doctor.Id.ToString(), doctor.Email, doctor.Name, "DOCTOR");
            var doctorResponse = new DoctorResponseDTO(doctor);

            return new DoctorAuthResultDTO
            {
                IsSuccess = true,
                Token = token,
                Data = new
                {
                    _id = doctorResponse.Id,
                    id = doctorResponse.Id,
                    email = doctorResponse.Email,
                    name = doctorResponse.Name,
                    specialization = doctorResponse.Specialization,
                    imageUrl = doctorResponse.ImageUrl,
                    availability = doctorResponse.Availability,
                    fee = doctorResponse.Fee
                }
            };
        }

        public async Task<DoctorAuthResultDTO> LoginDoctorAsync(DoctorLoginRequestDTO request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return new DoctorAuthResultDTO
                {
                    IsSuccess = false,
                    ErrorMessage = "Email and Password are required"
                };
            }

            var doctor = await _db.Doctors
                .FirstOrDefaultAsync(d => d.Email.ToLower() == request.Email.Trim().ToLower());

            if (doctor == null || !_passwordHasher.VerifyPassword(request.Password, doctor.Password))
            {
                return new DoctorAuthResultDTO
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid Email or Password"
                };
            }

            var token = _jwtTokenGenerator.GenerateToken(doctor.Id.ToString(), doctor.Email, doctor.Name, "DOCTOR");
            var doctorResponse = new DoctorResponseDTO(doctor);

            return new DoctorAuthResultDTO
            {
                IsSuccess = true,
                Token = token,
                Data = new
                {
                    _id = doctorResponse.Id,
                    id = doctorResponse.Id,
                    email = doctorResponse.Email,
                    name = doctorResponse.Name,
                    specialization = doctorResponse.Specialization,
                    imageUrl = doctorResponse.ImageUrl,
                    availability = doctorResponse.Availability,
                    fee = doctorResponse.Fee
                }
            };
        }
    }
}
