using backend_dotnet.Models.DTOs.User;

namespace backend_dotnet.Services.User
{
    public interface IUserService
    {
        Task<UserCountResultDTO> GetRegisteredUserCountAsync();
        Task<PatientProfileResultDTO> GetPatientProfileAsync(Guid authenticatedUserId);
        Task<UpdatePatientProfileResultDTO> UpdatePatientProfileAsync(Guid authenticatedUserId, UpdatePatientProfileRequestDTO request);
    }
}
