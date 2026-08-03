using backend_dotnet.Data;
using backend_dotnet.Models.DTOs.Service;
using backend_dotnet.Services.ImageUpload;
using System.Text.Json;

namespace backend_dotnet.Services.Service
{
    public class ServiceModuleService : IServiceModuleService
    {
        private readonly ApplicationDbContext _db;
        private readonly IImageUploadService _imageUploadService;

        public ServiceModuleService(ApplicationDbContext db, IImageUploadService imageUploadService)
        {
            _db = db;
            _imageUploadService = imageUploadService;
        }

        // Implementation of Service module methods will be defined here

        public async Task<ServiceResultDTO> CreateServiceAsync(CreateServiceRequestDTO createServiceRequestDTO, IFormFile? image)
        {
            if (string.IsNullOrWhiteSpace(createServiceRequestDTO.Name))
            {
                return new ServiceResultDTO
                {
                    IsSuccess = false,
                    ErrorMessage = "Name is required"
                };
            }

            var instructions = ParseJsonArrayField(createServiceRequestDTO.Instructions);
            var rawSlots = ParseJsonArrayField(createServiceRequestDTO.Slots);
            var slots = NormalizeSlotsToMap(rawSlots);
            var numericPrice = SanitizePrice(createServiceRequestDTO.Price);
            var available = ParseAvailability(createServiceRequestDTO.Availability);

            string? imageUrl = null;
            string? imagePublicId = null;

            if (image is not null && image.Length > 0)
            {
                try
                {
                    var uploadedUrl = await _imageUploadService.UploadImageAsync(image, "services");
                    if (!string.IsNullOrEmpty(uploadedUrl))
                    {
                        imageUrl = uploadedUrl;
                        // if your IImageUploadService also returns a public id, capture it here
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Cloudinary upload error: {ex.Message}");
                }
            }

            var service = new backend_dotnet.Models.Domain.Service
            {
                Name = createServiceRequestDTO.Name,
                About = createServiceRequestDTO.About ?? "",
                ShortDescription = createServiceRequestDTO.ShortDescription ?? "",
                Price = numericPrice,
                Available = available,
                Instructions = instructions.ToArray(),
                Slots = JsonSerializer.Serialize(slots),
                ImageUrl = imageUrl,
                ImagePublicId = imagePublicId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Services.Add(service);
            await _db.SaveChangesAsync();

            return new ServiceResultDTO
            {
                IsSuccess = true,
                Data = service
            };
        }

        private static List<string> ParseJsonArrayField(string? field)
        {
            if (string.IsNullOrWhiteSpace(field))
                return new List<string>();

            try
            {
                var parsed = JsonSerializer.Deserialize<List<string>>(field);
                return parsed ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        private static Dictionary<string, object> NormalizeSlotsToMap(List<string> rawSlots)
        {
            var map = new Dictionary<string, object>();
            for (int i = 0; i < rawSlots.Count; i++)
            {
                map[i.ToString()] = rawSlots[i];
            }
            return map;
        }

        private static decimal SanitizePrice(string? price)
        {
            if (string.IsNullOrWhiteSpace(price))
                return 0;

            return decimal.TryParse(price, out var result) ? result : 0;
        }

        private static bool ParseAvailability(string? availability)
        {
            if (string.IsNullOrWhiteSpace(availability))
                return true;

            return availability.Trim().ToLowerInvariant() switch
            {
                "true" or "available" or "1" => true,
                "false" or "unavailable" or "0" => false,
                _ => true
            };
        }
    }
}


