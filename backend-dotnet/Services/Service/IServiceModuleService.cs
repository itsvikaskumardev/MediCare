using backend_dotnet.Models.DTOs.Service;

namespace backend_dotnet.Services.Service
{
    public interface IServiceModuleService
    {
        // Service methods for Service module will be defined here
        Task<ServiceResultDTO> CreateServiceAsync(CreateServiceRequestDTO request, IFormFile? image);
        Task<ServiceResultDTO> GetServicesAsync();

        Task<ServiceResultDTO> GetServiceByIdAsync(Guid id);
        Task<ServiceResultDTO> UpdateServiceAsync(Guid id, UpdateServiceRequestDTO request, IFormFile? image);
        Task<ServiceResultDTO> DeleteServiceAsync(Guid id);
    }
}
