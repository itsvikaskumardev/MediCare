using backend_dotnet.Data;
using backend_dotnet.Models;
using backend_dotnet.Models.Domain;
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

        public async Task<DoctorAuthResultDTO> CreateDoctorAsync(CreateDoctorRequestDTO createDoctorRequestDTO, IFormFile? image)
        {
            if (string.IsNullOrWhiteSpace(createDoctorRequestDTO.Email) ||
                string.IsNullOrWhiteSpace(createDoctorRequestDTO.Password) ||
                string.IsNullOrWhiteSpace(createDoctorRequestDTO.Name))
            {
                return new DoctorAuthResultDTO
                {
                    IsSuccess = false,
                    ErrorMessage = "Email, Password and Name are required"
                };
            }

            var emailLc = createDoctorRequestDTO.Email.Trim().ToLowerInvariant();

            if (await _db.Doctors.AnyAsync(d => d.Email.ToLower() == emailLc))
            {
                return new DoctorAuthResultDTO
                {
                    IsSuccess = false,
                    ErrorMessage = "A doctor with this email already exists"
                };
            }

            string? imageUrl = createDoctorRequestDTO.ImageUrl;
            string? imagePublicId = createDoctorRequestDTO.ImagePublicId;

            if (image is not null && image.Length > 0)
            {
                var uploadedUrl = await _imageUploadService.UploadImageAsync(image, "medicare");
                if (!string.IsNullOrEmpty(uploadedUrl))
                {
                    imageUrl = uploadedUrl;
                }
            }

            var availability = Enum.TryParse<Availability>(createDoctorRequestDTO.Availability, true, out var parsedAvailability)
                ? parsedAvailability
                : Availability.Available;

            var passwordHash = _passwordHasher.HashPassword(createDoctorRequestDTO.Password);

            var doctor = new backend_dotnet.Models.Domain.Doctor
            {
                Email = emailLc,
                Password = passwordHash,
                Name = createDoctorRequestDTO.Name,
                Specialization = createDoctorRequestDTO.Specialization ?? "",
                ImageUrl = imageUrl,
                ImagePublicId = imagePublicId,
                Availability = availability,
                Experience = createDoctorRequestDTO.Experience ?? "",
                Qualifications = createDoctorRequestDTO.Qualifications ?? "",
                Location = createDoctorRequestDTO.Location ?? "",
                About = createDoctorRequestDTO.About ?? "",
                Fee = createDoctorRequestDTO.Fee ?? 0,
                Schedule = string.IsNullOrWhiteSpace(createDoctorRequestDTO.Schedule) ? "{}" : createDoctorRequestDTO.Schedule,
                Success = createDoctorRequestDTO.Success ?? "",
                Patients = createDoctorRequestDTO.Patients ?? "",
                Rating = createDoctorRequestDTO.Rating ?? 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
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
