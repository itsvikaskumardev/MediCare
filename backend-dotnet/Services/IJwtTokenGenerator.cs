using backend_dotnet.Models;

namespace backend_dotnet.Services
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(User user);
    }
}
