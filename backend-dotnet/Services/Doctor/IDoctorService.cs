using backend_dotnet.Models.DTOs.Doctor;

namespace backend_dotnet.Services.Doctor
{
    public interface IDoctorService
    {
        Task<DoctorAuthResultDTO> CreateDoctorAsync(CreateDoctorRequestDTO request, IFormFile? image);
        Task<DoctorListResultDTO> GetDoctorsAsync(GetDoctorsRequestDTO request);
        Task<DoctorSingleResultDTO> GetDoctorByIdAsync(Guid id);
        Task<DoctorUpdateResultDTO> UpdateDoctorAsync(Guid id, UpdateDoctorRequestDTO request, IFormFile? image);
        Task<DoctorDeleteResultDTO> DeleteDoctorAsync(Guid id);
        Task<DoctorToggleAvailabilityResultDTO> ToggleAvailabilityAsync(Guid id);
    }
}
