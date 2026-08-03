using backend_dotnet.Models.DTOs.Service;

namespace backend_dotnet.Services.Service
{
    public interface IServiceModuleService
    {
        // Service methods for Service module will be defined here
        Task<ServiceResultDTO> CreateServiceAsync(CreateServiceRequestDTO request, IFormFile? image);
    }
}
