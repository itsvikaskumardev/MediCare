using backend_dotnet.Models.DTOs.User;
using System.Text.Json;

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
                var totalCount = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(_db.Users);

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
    }
}
