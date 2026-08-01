using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using backend_dotnet.Models;
using Microsoft.IdentityModel.Tokens;

namespace backend_dotnet.Services
{
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly IConfiguration _configuration;

        public JwtTokenGenerator(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(User user)
        {
            var secret = _configuration["ApiSettings:Secret"]
                         ?? throw new InvalidOperationException("JWT Secret is not configured in ApiSettings:Secret");
            var key = Encoding.ASCII.GetBytes(secret);

            var issuer = _configuration["ApiSettings:Issuer"];
            var audience = _configuration["ApiSettings:Audience"];
            var expiryMinutesStr = _configuration["ApiSettings:TokenExpiryInMinutes"];
            var expiryMinutes = double.TryParse(expiryMinutesStr, out var minutes) ? minutes : 60;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            if (!string.IsNullOrWhiteSpace(issuer))
            {
                tokenDescriptor.Issuer = issuer;
            }

            if (!string.IsNullOrWhiteSpace(audience))
            {
                tokenDescriptor.Audience = audience;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
