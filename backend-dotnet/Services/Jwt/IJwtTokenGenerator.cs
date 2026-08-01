namespace backend_dotnet.Services.Jwt
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(string id, string email, string name, string role);
    }
}
