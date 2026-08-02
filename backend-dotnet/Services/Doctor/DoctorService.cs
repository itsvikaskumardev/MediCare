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

        //-------------------------------------------LoginDoctor-----------------------------------------------------

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


        //-------------------------------------------GetDoctors-----------------------------------------------------

        public async Task<DoctorListResultDTO> GetDoctorsAsync(GetDoctorsRequestDTO getDoctorsRequestDTO)
        {
            try
            {
                var limit = Math.Min(500, Math.Max(1, getDoctorsRequestDTO.Limit ?? 200));
                var page = Math.Max(1, getDoctorsRequestDTO.Page ?? 1);
                var skip = (page - 1) * limit;

                var query = _db.Doctors.AsQueryable();

                if (!string.IsNullOrWhiteSpace(getDoctorsRequestDTO.Q))
                {
                    var q = getDoctorsRequestDTO.Q.Trim();
                    query = query.Where(d =>
                        EF.Functions.ILike(d.Name, $"%{q}%") ||
                        (d.Specialization != null && EF.Functions.ILike(d.Specialization, $"%{q}%")) ||
                        EF.Functions.ILike(d.Email, $"%{q}%"));
                }

                var total = await query.CountAsync();

                var docs = await query
                    .OrderBy(d => d.Name)
                    .Skip(skip)
                    .Take(limit)
                    .Select(d => new
                    {
                        d.Id,
                        d.Name,
                        d.Specialization,
                        d.Fee,
                        d.ImageUrl,
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
        public async Task<DoctorSingleResultDTO> GetDoctorByIdAsync(string id)
        {
            try
            {
                if (!Guid.TryParse(id, out var doctorId))
                {
                    return new DoctorSingleResultDTO { IsSuccess = false, ErrorMessage = "Doctor not found" };
                }

                var doctor = await _db.Doctors
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == doctorId);

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
        public async Task<DoctorUpdateResultDTO> UpdateDoctorAsync(string id, UpdateDoctorRequestDTO updateDoctorRequestDTO, IFormFile? image)
        {
            try
            {
                if (!Guid.TryParse(id, out var doctorId))
                {
                    return new DoctorUpdateResultDTO { IsSuccess = false, ErrorMessage = "Doctor not found" };
                }

                var existing = await _db.Doctors.FirstOrDefaultAsync(d => d.Id == doctorId);
                if (existing is null)
                {
                    return new DoctorUpdateResultDTO { IsSuccess = false, ErrorMessage = "Doctor not found" };
                }

                // Image handling
                if (image is not null && image.Length > 0)
                {
                    var uploaded = await _imageUploadService.UploadImageAsync(image, "medicare");
                    if (!string.IsNullOrEmpty(uploaded))
                    {
                        var previousPublicId = existing.ImagePublicId;
                        existing.ImageUrl = uploaded;

                        if (!string.IsNullOrEmpty(previousPublicId) && previousPublicId != existing.ImagePublicId)
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
                    existing.ImageUrl = updateDoctorRequestDTO.ImageUrl;
                }

                // Schedule
                if (!string.IsNullOrWhiteSpace(updateDoctorRequestDTO.Schedule))
                {
                    existing.Schedule = updateDoctorRequestDTO.Schedule;
                }

                // Simple updatable fields
                if (updateDoctorRequestDTO.Name is not null) existing.Name = updateDoctorRequestDTO.Name;
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
                    if (emailLc != existing.Email)
                    {
                        var other = await _db.Doctors.FirstOrDefaultAsync(d => d.Email.ToLower() == emailLc);
                        if (other is not null && other.Id != doctorId)
                        {
                            return new DoctorUpdateResultDTO { IsSuccess = false, ErrorMessage = "Email already in use" };
                        }
                        existing.Email = emailLc;
                    }
                }

                // Password
                if (!string.IsNullOrWhiteSpace(updateDoctorRequestDTO.Password))
                {
                    existing.Password = _passwordHasher.HashPassword(updateDoctorRequestDTO.Password);
                }

                existing.UpdatedAt = DateTime.UtcNow;

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
    }
}










/*
 --------------------------------------------------------------------------------------------------------------------

`AnyAsync()` and `FirstOrDefaultAsync()` are both asynchronous, but they are used for different purposes.

---------------------------------
In your code:await _db.Doctors.AnyAsync(d => d.Email.ToLower() == emailLc

AnyAsync(): You only want to know **whether a doctor with that email exists**.

`AnyAsync()` returns a `bool`:

* `true` → a matching record exists.
* `false` → no matching record exists.
* * ✅ Less data transferred.
* ✅ Expresses your intent clearly: "Does a record exist?"

It does **not** retrieve the full doctor object, making it more efficient.

---------------------------------


FirstOrDefaultAsync():

var doctor = await _db.Doctors
    .FirstOrDefaultAsync(d => d.Email.ToLower() == emailLc);

if (doctor != null){    // Email already exists}

This also works, but it fetches the entire `Doctor` entity from the database, even though you only need to know if it exists.

* Retrieves the first matching `Doctor` entity.
* Uses more memory and transfers more data than necessary.
* Better when you actually need to use the doctor's properties.


 
 --------------------------------------------------------------------------------------------------------------------
 
 
 
 
 
 
 
 
 
 */