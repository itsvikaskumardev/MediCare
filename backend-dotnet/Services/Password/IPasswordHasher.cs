namespace backend_dotnet.Services.Password
{
    public interface IPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string inputPassword, string storedHash);
    }
}
