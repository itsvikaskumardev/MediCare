using backend_dotnet.Models.DTOs.User;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace backend_dotnet.Services.User
{
    public class UserService : IUserService
    {
        private readonly backend_dotnet.Data.ApplicationDbContext _db;

        public UserService(backend_dotnet.Data.ApplicationDbContext db)
        {
            _db = db;
        }

        //-------------------------------GetRegisteredUserCountAsync------------------------------------------------------

        public async Task<UserCountResultDTO> GetRegisteredUserCountAsync()
        {
            try
            {
                var totalPatients = await _db.Users.CountAsync(u => u.Role == Role.PATIENT && u.IsActive && !u.IsDeleted);
                var totalAdmins = await _db.Users.CountAsync(u => u.Role == Role.ADMIN && u.IsActive && !u.IsDeleted);
                var totalUsers = totalPatients + totalAdmins;

                return new UserCountResultDTO
                {
                    IsSuccess = true,
                    TotalUsers = totalUsers,
                    TotalPatients = totalPatients,
                    TotalAdmins = totalAdmins
                };
            }
            catch (Exception ex)
            {
                return new UserCountResultDTO { IsSuccess = false, ErrorMessage = ex.Message };
            }
        }

        //-------------------------------GetPatientProfileAsync------------------------------------------------------

        public async Task<PatientProfileResultDTO> GetPatientProfileAsync(Guid authenticatedUserId)
        {
            if (authenticatedUserId == Guid.Empty)
            {
                return new PatientProfileResultDTO { IsSuccess = false, StatusCode = System.Net.HttpStatusCode.Unauthorized, ErrorMessage = "Unauthorized" };
            }

            var user = await _db.Users
                .Where(u => u.IsActive && !u.IsDeleted)
                .Include(u => u.PatientProfile)
                .FirstOrDefaultAsync(u => u.Id == authenticatedUserId);
            if (user == null)
            {
                return new PatientProfileResultDTO { IsSuccess = false, StatusCode = System.Net.HttpStatusCode.NotFound, ErrorMessage = "User not found" };
            }

            return new PatientProfileResultDTO
            {
                IsSuccess = true,
                Profile = new PatientProfileDTO
                {
                    Name = user.Name,
                    Email = user.Email,
                    Mobile = user.Mobile,
                    Age = user.Age,
                    Gender = user.Gender,
                    ImageUrl = user.ImageUrl,
                    ImagePublicId = user.ImagePublicId,
                    BloodGroup = user.PatientProfile?.BloodGroup,
                    MedicalHistory = user.PatientProfile?.MedicalHistory,
                    Allergies = user.PatientProfile?.Allergies,
                    EmergencyContactName = user.PatientProfile?.EmergencyContactName,
                    EmergencyContactNumber = user.PatientProfile?.EmergencyContactNumber,
                    InsuranceProvider = user.PatientProfile?.InsuranceProvider,
                    InsurancePolicyNumber = user.PatientProfile?.InsurancePolicyNumber
                }
            };
        }

        //-------------------------------UpdatePatientProfileAsync------------------------------------------------------

        public async Task<UpdatePatientProfileResultDTO> UpdatePatientProfileAsync(Guid authenticatedUserId, UpdatePatientProfileRequestDTO request)
        {
            if (authenticatedUserId == Guid.Empty)
            {
                return new UpdatePatientProfileResultDTO { IsSuccess = false, StatusCode = System.Net.HttpStatusCode.Unauthorized, ErrorMessage = "Unauthorized" };
            }

            var user = await _db.Users
                .Where(u => u.IsActive && !u.IsDeleted)
                .Include(u => u.PatientProfile)
                .FirstOrDefaultAsync(u => u.Id == authenticatedUserId);
            if (user == null)
            {
                return new UpdatePatientProfileResultDTO { IsSuccess = false, StatusCode = System.Net.HttpStatusCode.NotFound, ErrorMessage = "User not found" };
            }

            if (request.Name != null) user.Name = request.Name;
            if (request.Mobile != null) user.Mobile = request.Mobile;
            if (request.Age != null) user.Age = request.Age.Value;
            if (request.Gender != null) user.Gender = request.Gender;

            if (user.PatientProfile == null)
            {
                user.PatientProfile = new backend_dotnet.Models.Domain.Patient { UserId = user.Id };
                _db.Patients.Add(user.PatientProfile);
            }

            if (request.BloodGroup != null) user.PatientProfile.BloodGroup = request.BloodGroup;
            if (request.MedicalHistory != null) user.PatientProfile.MedicalHistory = request.MedicalHistory;
            if (request.Allergies != null) user.PatientProfile.Allergies = request.Allergies;
            if (request.EmergencyContactName != null) user.PatientProfile.EmergencyContactName = request.EmergencyContactName;
            if (request.EmergencyContactNumber != null) user.PatientProfile.EmergencyContactNumber = request.EmergencyContactNumber;
            if (request.InsuranceProvider != null) user.PatientProfile.InsuranceProvider = request.InsuranceProvider;
            if (request.InsurancePolicyNumber != null) user.PatientProfile.InsurancePolicyNumber = request.InsurancePolicyNumber;

            await _db.SaveChangesAsync();

            return new UpdatePatientProfileResultDTO
            {
                IsSuccess = true,
                Profile = new PatientProfileDTO
                {
                    Name = user.Name,
                    Email = user.Email,
                    Mobile = user.Mobile,
                    Age = user.Age,
                    Gender = user.Gender,
                    ImageUrl = user.ImageUrl,
                    ImagePublicId = user.ImagePublicId,
                    BloodGroup = user.PatientProfile?.BloodGroup,
                    MedicalHistory = user.PatientProfile?.MedicalHistory,
                    Allergies = user.PatientProfile?.Allergies,
                    EmergencyContactName = user.PatientProfile?.EmergencyContactName,
                    EmergencyContactNumber = user.PatientProfile?.EmergencyContactNumber,
                    InsuranceProvider = user.PatientProfile?.InsuranceProvider,
                    InsurancePolicyNumber = user.PatientProfile?.InsurancePolicyNumber
                }
            };
        }

        //-------------------------------GetAdminProfileAsync------------------------------------------------------

        public async Task<AdminProfileResultDTO> GetAdminProfileAsync(Guid authenticatedUserId)
        {
            if (authenticatedUserId == Guid.Empty)
            {
                return new AdminProfileResultDTO { IsSuccess = false, StatusCode = System.Net.HttpStatusCode.Unauthorized, ErrorMessage = "Unauthorized" };
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == authenticatedUserId && u.Role == Role.ADMIN && u.IsActive && !u.IsDeleted);
            if (user == null)
            {
                return new AdminProfileResultDTO { IsSuccess = false, StatusCode = System.Net.HttpStatusCode.NotFound, ErrorMessage = "Admin not found" };
            }

            return new AdminProfileResultDTO
            {
                IsSuccess = true,
                StatusCode = System.Net.HttpStatusCode.OK,
                Profile = new AdminProfileDTO
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Role = "ADMIN",
                    ImageUrl = user.ImageUrl
                }
            };
        }

        //-------------------------------UpdateAdminProfileAsync------------------------------------------------------

        public async Task<AdminProfileResultDTO> UpdateAdminProfileAsync(Guid authenticatedUserId, UpdateAdminProfileRequestDTO request)
        {
            if (authenticatedUserId == Guid.Empty)
            {
                return new AdminProfileResultDTO { IsSuccess = false, StatusCode = System.Net.HttpStatusCode.Unauthorized, ErrorMessage = "Unauthorized" };
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == authenticatedUserId && u.Role == Role.ADMIN && u.IsActive && !u.IsDeleted);
            if (user == null)
            {
                return new AdminProfileResultDTO { IsSuccess = false, StatusCode = System.Net.HttpStatusCode.NotFound, ErrorMessage = "Admin not found" };
            }

            if (!string.IsNullOrEmpty(request.Name)) user.Name = request.Name;
            if (!string.IsNullOrEmpty(request.Email)) user.Email = request.Email;
            if (request.ImageUrl != null) user.ImageUrl = request.ImageUrl; // Assuming ImageUrl is handled here

            user.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return new AdminProfileResultDTO
            {
                IsSuccess = true,
                StatusCode = System.Net.HttpStatusCode.OK,
                Profile = new AdminProfileDTO
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Role = "ADMIN",
                    ImageUrl = user.ImageUrl
                }
            };
        }
    }
}
