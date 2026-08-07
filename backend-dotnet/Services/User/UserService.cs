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

        public async Task<UserCountResultDTO> GetRegisteredUserCountAsync()
        {
            try
            {
                var totalCount = await _db.Users.CountAsync();

                return new UserCountResultDTO
                {
                    IsSuccess = true,
                    TotalUsers = totalCount
                };
            }
            catch (Exception ex)
            {
                return new UserCountResultDTO
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<PatientProfileResultDTO> GetPatientProfileAsync(Guid authenticatedUserId)
        {
            if (authenticatedUserId == Guid.Empty)
            {
                return new PatientProfileResultDTO { IsSuccess = false, StatusCode = System.Net.HttpStatusCode.Unauthorized, ErrorMessage = "Unauthorized" };
            }

            var user = await _db.Users
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

        public async Task<UpdatePatientProfileResultDTO> UpdatePatientProfileAsync(Guid authenticatedUserId, UpdatePatientProfileRequestDTO request)
        {
            if (authenticatedUserId == Guid.Empty)
            {
                return new UpdatePatientProfileResultDTO { IsSuccess = false, StatusCode = System.Net.HttpStatusCode.Unauthorized, ErrorMessage = "Unauthorized" };
            }

            var user = await _db.Users
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
    }
}
