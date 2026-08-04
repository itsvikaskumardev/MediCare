using backend_dotnet.Models.DTOs.User;
using System.Text.Json;

namespace backend_dotnet.Services.User
{
    public class UserService : IUserService
    {
        private readonly HttpClient _httpClient;
        private readonly string? _clerkSecretKey;

        public UserService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _clerkSecretKey = config["Clerk:SecretKey"];
        }

        public async Task<UserCountResultDTO> GetRegisteredUserCountAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.clerk.com/v1/users/count");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _clerkSecretKey);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return new UserCountResultDTO
                {
                    IsSuccess = false,
                    ErrorMessage = $"Clerk API returned {(int)response.StatusCode}"
                };
            }

            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var totalCount = doc.RootElement.GetProperty("total_count").GetInt32();

            return new UserCountResultDTO
            {
                IsSuccess = true,
                TotalUsers = totalCount
            };
        }
    }
}
