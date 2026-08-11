using System.Text.Json;
using backend_dotnet.Data;
using backend_dotnet.Models;
using backend_dotnet.Models.Domain;
using backend_dotnet.Models.DTOs.Doctor;
using backend_dotnet.Services.ImageUpload;
using backend_dotnet.Services.Jwt;
using backend_dotnet.Services.Password;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace backend_dotnet.Services.Doctor
{
    public class DoctorService : IDoctorService
    {
        private readonly ApplicationDbContext _db;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IImageUploadService _imageUploadService;
        private readonly ILogger<DoctorService> _logger;

        public DoctorService(
            ApplicationDbContext db,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator,
            IImageUploadService imageUploadService,
            ILogger<DoctorService> logger)
        {
            _db = db;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
            _imageUploadService = imageUploadService;
            _logger = logger;
        }


        //-------------------------------------------CreateDoctor-----------------------------------------------------

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

            if (await _db.Users.AnyAsync(u => u.Email.ToLower() == emailLc))
            {
                return new DoctorAuthResultDTO
                {
                    IsSuccess = false,
                    ErrorMessage = "A user with this email already exists"
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

            // 1. Create the User identity
            var user = new backend_dotnet.Models.Domain.User
            {
                Email = emailLc,
                PasswordHash = passwordHash,
                Name = createDoctorRequestDTO.Name,
                ImageUrl = imageUrl,
                ImagePublicId = imagePublicId,
                Role = Role.DOCTOR,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            _db.Users.Add(user);// Adds one entity to the DbContext.

            // 2. Create the Doctor profile linked to the User
            var doctor = new backend_dotnet.Models.Domain.Doctor
            {
                UserId = user.Id, // Link to User
                Specialization = createDoctorRequestDTO.Specialization ?? "",
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
                User = user
            };

            _db.Doctors.Add(doctor);
            await _db.SaveChangesAsync();

            var token = _jwtTokenGenerator.GenerateToken(user.Id.ToString(), user.Email, user.Name, "DOCTOR");
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


        //-------------------------------------------GetDoctors-----------------------------------------------------

        public async Task<DoctorListResultDTO> GetDoctorsAsync(GetDoctorsRequestDTO getDoctorsRequestDTO)
        {
            try
            {
                var limit = Math.Min(500, Math.Max(1, getDoctorsRequestDTO.Limit ?? 200));
                var page = Math.Max(1, getDoctorsRequestDTO.Page ?? 1);
                var skip = (page - 1) * limit;

                var query = _db.Doctors
                    .Where(d => d.IsActive && !d.IsDeleted)
                    .Include(d => d.User)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(getDoctorsRequestDTO.Q))
                {
                    var q = getDoctorsRequestDTO.Q.Trim();
                    query = query.Where(d =>
                        EF.Functions.ILike(d.User.Name, $"%{q}%") ||
                        (d.Specialization != null && EF.Functions.ILike(d.Specialization, $"%{q}%")) ||
                        EF.Functions.ILike(d.User.Email, $"%{q}%"));
                }

                var total = await query.CountAsync();

                var docs = await query
                    .OrderBy(d => d.User.Name)
                    .Skip(skip)
                    .Take(limit)
                    .Select(d => new
                    {
                        d.Id,
                        Name = d.User.Name,
                        Email = d.User.Email,
                        d.Specialization,
                        d.Fee,
                        ImageUrl = d.User.ImageUrl,
                        d.Availability,
                        d.Schedule,
                        d.Patients,
                        d.Rating,
                        d.About,
                        d.Experience,
                        d.Qualifications,
                        d.Location,
                        d.Success,
                        AppointmentsTotal = _db.Appointments.Count(a => a.DoctorId == d.Id),
                        AppointmentsCompleted = _db.Appointments.Count(a =>
                            a.DoctorId == d.Id && (a.Status == AppointmentStatus.Confirmed || a.Status == AppointmentStatus.Completed)),
                        AppointmentsCanceled = _db.Appointments.Count(a =>
                            a.DoctorId == d.Id && a.Status == AppointmentStatus.Canceled),
                        Earnings = _db.Appointments
                            .Where(a => a.DoctorId == d.Id && (a.Status == AppointmentStatus.Confirmed || a.Status == AppointmentStatus.Completed))
                            .Sum(a => (decimal?)a.Fees) ?? 0
                    })
                    .ToListAsync();

                var data = docs.Select(d => new DoctorListItemDTO
                {
                    Id = d.Id.ToString(),
                    Name = d.Name ?? "",
                    Specialization = d.Specialization ?? "",
                    Fee = d.Fee,
                    ImageUrl = d.ImageUrl,
                    AppointmentsTotal = d.AppointmentsTotal,
                    AppointmentsCompleted = d.AppointmentsCompleted,
                    AppointmentsCanceled = d.AppointmentsCanceled,
                    Earnings = d.Earnings,
                    Availability = d.Availability.ToString(),
                    Schedule = string.IsNullOrWhiteSpace(d.Schedule)
                        ? new { }
                        : JsonSerializer.Deserialize<object>(d.Schedule) ?? new { },
                    Patients = d.Patients ?? "",
                    Rating = d.Rating,
                    About = d.About ?? "",
                    Experience = d.Experience ?? "",
                    Qualifications = d.Qualifications ?? "",
                    Location = d.Location ?? "",
                    Success = d.Success ?? ""
                }).ToList();

                return new DoctorListResultDTO
                {
                    IsSuccess = true,
                    Data = data,
                    Page = page,
                    Limit = limit,
                    Total = total
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetDoctorsAsync error");
                return new DoctorListResultDTO { IsSuccess = false, ErrorMessage = "Server error" };
            }
        }

        //-------------------------------------------GetDoctorById-----------------------------------------------------
        public async Task<DoctorSingleResultDTO> GetDoctorByIdAsync(Guid id)
        {
            try
            {
                var doctor = await _db.Doctors
                    .Where(d => d.IsActive && !d.IsDeleted)
                    .Include(d => d.User)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == id || d.UserId == id);

                if (doctor is null)
                {
                    return new DoctorSingleResultDTO { IsSuccess = false, ErrorMessage = "Doctor not found" };
                }

                var doctorResponse = new DoctorResponseDTO(doctor);

                return new DoctorSingleResultDTO
                {
                    IsSuccess = true,
                    Data = doctorResponse
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetDoctorByIdAsync error");
                return new DoctorSingleResultDTO { IsSuccess = false, ErrorMessage = "Server error" };
            }
        }

        //-------------------------------------------UpdateDoctor-----------------------------------------------------
        public async Task<DoctorUpdateResultDTO> UpdateDoctorAsync(Guid id, UpdateDoctorRequestDTO updateDoctorRequestDTO, IFormFile? image)
        {
            try
            {
                var existing = await _db.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == id || d.UserId == id);
                if (existing is null)
                {
                    return new DoctorUpdateResultDTO { IsSuccess = false, ErrorMessage = "Doctor not found" };
                }

                // Image handling on User
                if (image is not null && image.Length > 0)
                {
                    var uploaded = await _imageUploadService.UploadImageAsync(image, "medicare");
                    if (!string.IsNullOrEmpty(uploaded))
                    {
                        var previousPublicId = existing.User.ImagePublicId;
                        existing.User.ImageUrl = uploaded;

                        if (!string.IsNullOrEmpty(previousPublicId) && previousPublicId != existing.User.ImagePublicId)
                        {
                            _ = _imageUploadService.DeleteImageAsync(previousPublicId)
                                .ContinueWith(t =>
                                {
                                    if (t.Exception is not null)
                                        _logger.LogWarning(t.Exception, "deleteFromCloudinary warning");
                                }, TaskScheduler.Default);
                        }
                    }
                }
                else if (!string.IsNullOrWhiteSpace(updateDoctorRequestDTO.ImageUrl))
                {
                    existing.User.ImageUrl = updateDoctorRequestDTO.ImageUrl;
                }

                // Schedule
                if (!string.IsNullOrWhiteSpace(updateDoctorRequestDTO.Schedule))
                {
                    existing.Schedule = updateDoctorRequestDTO.Schedule;
                }

                // Updatable fields
                if (updateDoctorRequestDTO.Name is not null) existing.User.Name = updateDoctorRequestDTO.Name;
                if (updateDoctorRequestDTO.Specialization is not null) existing.Specialization = updateDoctorRequestDTO.Specialization;
                if (updateDoctorRequestDTO.Experience is not null) existing.Experience = updateDoctorRequestDTO.Experience;
                if (updateDoctorRequestDTO.Qualifications is not null) existing.Qualifications = updateDoctorRequestDTO.Qualifications;
                if (updateDoctorRequestDTO.Location is not null) existing.Location = updateDoctorRequestDTO.Location;
                if (updateDoctorRequestDTO.About is not null) existing.About = updateDoctorRequestDTO.About;
                if (updateDoctorRequestDTO.Fee.HasValue) existing.Fee = updateDoctorRequestDTO.Fee.Value;
                if (updateDoctorRequestDTO.Success is not null) existing.Success = updateDoctorRequestDTO.Success;
                if (updateDoctorRequestDTO.Patients is not null) existing.Patients = updateDoctorRequestDTO.Patients;
                if (updateDoctorRequestDTO.Rating.HasValue) existing.Rating = (decimal)updateDoctorRequestDTO.Rating.Value;

                if (!string.IsNullOrWhiteSpace(updateDoctorRequestDTO.Availability) &&
                    Enum.TryParse<Availability>(updateDoctorRequestDTO.Availability, true, out var parsedAvailability))
                {
                    existing.Availability = parsedAvailability;
                }

                // Email uniqueness check
                if (!string.IsNullOrWhiteSpace(updateDoctorRequestDTO.Email))
                {
                    var emailLc = updateDoctorRequestDTO.Email.Trim().ToLowerInvariant();
                    if (emailLc != existing.User.Email)
                    {
                        var other = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == emailLc);
                        if (other is not null && other.Id != existing.User.Id)
                        {
                            return new DoctorUpdateResultDTO { IsSuccess = false, ErrorMessage = "Email already in use" };
                        }
                        existing.User.Email = emailLc;
                    }
                }

                // Password
                if (!string.IsNullOrWhiteSpace(updateDoctorRequestDTO.Password))
                {
                    existing.User.PasswordHash = _passwordHasher.HashPassword(updateDoctorRequestDTO.Password);
                }

                existing.User.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                var doctorResponse = new DoctorResponseDTO(existing);

                return new DoctorUpdateResultDTO
                {
                    IsSuccess = true,
                    Data = doctorResponse
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateDoctorAsync error");
                return new DoctorUpdateResultDTO { IsSuccess = false, ErrorMessage = "Server error" };
            }
        }

        //-------------------------------------------DeleteDoctor-----------------------------------------------------
        public async Task<DoctorDeleteResultDTO> DeleteDoctorAsync(Guid id)
        {
            try
            {
                var existing = await _db.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == id || d.UserId == id);
                if (existing is null)
                {
                    return new DoctorDeleteResultDTO { IsSuccess = false, ErrorMessage = "Doctor not found" };
                }

                if (!string.IsNullOrEmpty(existing.User.ImagePublicId))
                {
                    try
                    {
                        await _imageUploadService.DeleteImageAsync(existing.User.ImagePublicId);
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning(e, "deleteFromCloudinary warning");
                    }
                }

                existing.IsDeleted = true;
                await _db.SaveChangesAsync();

                return new DoctorDeleteResultDTO { IsSuccess = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteDoctorAsync error");
                return new DoctorDeleteResultDTO { IsSuccess = false, ErrorMessage = "Server error" };
            }
        }

        //-------------------------------------------ToggleAvailability-----------------------------------------------------
        public async Task<DoctorToggleAvailabilityResultDTO> ToggleAvailabilityAsync(Guid id)
        {
            try
            {
                var doctor = await _db.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == id || d.UserId == id);
                if (doctor is null)
                {
                    return new DoctorToggleAvailabilityResultDTO { IsSuccess = false, ErrorMessage = "Doctor not found" };
                }

                doctor.Availability = doctor.Availability == Availability.Available
                    ? Availability.Unavailable
                    : Availability.Available;

                doctor.User.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                var doctorResponse = new DoctorResponseDTO(doctor);

                return new DoctorToggleAvailabilityResultDTO
                {
                    IsSuccess = true,
                    Data = doctorResponse
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ToggleAvailabilityAsync error");
                return new DoctorToggleAvailabilityResultDTO { IsSuccess = false, ErrorMessage = "Server error" };
            }
        }
    }
}