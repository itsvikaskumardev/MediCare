using backend_dotnet.Models.DTOs.User;

namespace backend_dotnet.Services.User
{
    public interface IUserService
    {
        Task<UserCountResultDTO> GetRegisteredUserCountAsync();
    }
}
