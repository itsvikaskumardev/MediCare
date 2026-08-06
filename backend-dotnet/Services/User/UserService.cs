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
    }
}
