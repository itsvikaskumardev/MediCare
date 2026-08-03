using backend_dotnet.Data;
using backend_dotnet.Models.DTOs.Service;
using backend_dotnet.Services.ImageUpload;
using Microsoft.EntityFrameworkCore;
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

        //-----------------------------------CreateService--------------------------------------------

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

        //-----------------------------------GetService--------------------------------------------

        public async Task<ServiceResultDTO> GetServicesAsync()
        {
            var list = await _db.Services
                .AsNoTracking()
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return new ServiceResultDTO
            {
                IsSuccess = true,
                Data = list
            };
        }

        //-----------------------------------GetServiceById--------------------------------------------

        public async Task<ServiceResultDTO> GetServiceByIdAsync(Guid id)
        {
            var service = await _db.Services
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id); // dont use findasync 

            if (service is null)
            {
                return new ServiceResultDTO
                {
                    IsSuccess = false,
                    ErrorMessage = "Service not found"
                };
            }

            return new ServiceResultDTO
            {
                IsSuccess = true,
                Data = service
            };
        }

        //-----------------------------------UpdateService--------------------------------------------
        public async Task<ServiceResultDTO> UpdateServiceAsync(Guid id, UpdateServiceRequestDTO updateServiceRequestDTO, IFormFile? image)
        {
            var existing = await _db.Services.FindAsync(id);

            if (existing is null)
            {
                return new ServiceResultDTO
                {
                    IsSuccess = false,
                    ErrorMessage = "Service not found"
                };
            }

            if (updateServiceRequestDTO.Name is not null)
                existing.Name = updateServiceRequestDTO.Name;

            if (updateServiceRequestDTO.About is not null)
                existing.About = updateServiceRequestDTO.About;

            if (updateServiceRequestDTO.ShortDescription is not null)
                existing.ShortDescription = updateServiceRequestDTO.ShortDescription;

            if (updateServiceRequestDTO.Price is not null)
                existing.Price = SanitizePrice(updateServiceRequestDTO.Price);

            if (updateServiceRequestDTO.Availability is not null)
                existing.Available = ParseAvailability(updateServiceRequestDTO.Availability);

            if (updateServiceRequestDTO.Instructions is not null)
                existing.Instructions = ParseJsonArrayField(updateServiceRequestDTO.Instructions).ToArray();

            if (updateServiceRequestDTO.Slots is not null)
            {
                var rawSlots = ParseJsonArrayField(updateServiceRequestDTO.Slots);
                existing.Slots = JsonSerializer.Serialize(NormalizeSlotsToMap(rawSlots));
            }

            if (image is not null && image.Length > 0)
            {
                try
                {
                    var uploadedUrl = await _imageUploadService.UploadImageAsync(image, "services");
                    if (!string.IsNullOrEmpty(uploadedUrl))
                    {
                        var oldPublicId = existing.ImagePublicId;

                        existing.ImageUrl = uploadedUrl;
                        existing.ImagePublicId = null; // set this from your upload result if it returns a public id

                        if (!string.IsNullOrEmpty(oldPublicId))
                        {
                            try
                            {
                                await _imageUploadService.DeleteImageAsync(oldPublicId);
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine($"Cloudinary delete failed: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Cloudinary upload error: {ex.Message}");
                }
            }

            existing.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return new ServiceResultDTO
            {
                IsSuccess = true,
                Data = existing
            };
        }

        //-----------------------------------DeleteService--------------------------------------------

        public async Task<ServiceResultDTO> DeleteServiceAsync(Guid id)
        {
            var existing = await _db.Services.FindAsync(id);

            if (existing is null)
            {
                return new ServiceResultDTO
                {
                    IsSuccess = false,
                    ErrorMessage = "Service not found"
                };
            }

            if (!string.IsNullOrEmpty(existing.ImagePublicId))
            {
                try
                {
                    await _imageUploadService.DeleteImageAsync(existing.ImagePublicId);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"failed to delete cloud image on service delete: {ex.Message}");
                }
            }

            _db.Services.Remove(existing);
            await _db.SaveChangesAsync();

            return new ServiceResultDTO
            {
                IsSuccess = true
            };
        }








        //-----------------------------------Helper Functions --------------------------------------------

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



/*
 
 
 
 
 
 Here is why they are different and why .AsNoTracking().FirstOrDefaultAsync(...) is usually preferred for read-only (GET) requests:

.AsNoTracking() for Better Read-Only Performance:

By calling .AsNoTracking(), Entity Framework Core does not store the fetched entity in its Change Tracker.
This reduces memory footprint and makes GET queries significantly faster when you don't intend to modify or update the entity in the database.
Why not FindAsync with .AsNoTracking()?

.FindAsync(...) is a method on DbSet<T> that checks EF Core's in-memory Change Tracker first before querying the database.
Once you call .AsNoTracking(), the return type becomes an IQueryable<T>, which does not have .FindAsync. Therefore, .FirstOrDefaultAsync(d => d.Id == id) is required.
When to use FindAsync:

Use FindAsync(id) when you plan to modify/update or delete the entity immediately afterward (e.g., in UpdateDoctor or DeleteService), because EF Core needs to track the entity to save changes.
For read-only endpoints like GetServiceById, both work, but .AsNoTracking().FirstOrDefaultAsync(s => s.Id == id) is the recommended best practice.
 
 
 
 
 
 
 
 
 
 
 
 
 
 */