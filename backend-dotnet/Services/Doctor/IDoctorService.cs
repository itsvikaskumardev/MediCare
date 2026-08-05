using backend_dotnet.Models.DTOs.Doctor;

namespace backend_dotnet.Services.Doctor
{
    public interface IDoctorService
    {
        Task<DoctorAuthResultDTO> CreateDoctorAsync(CreateDoctorRequestDTO request, IFormFile? image);
        Task<DoctorListResultDTO> GetDoctorsAsync(GetDoctorsRequestDTO request);
        Task<DoctorSingleResultDTO> GetDoctorByIdAsync(string id);
        Task<DoctorUpdateResultDTO> UpdateDoctorAsync(string id, UpdateDoctorRequestDTO request, IFormFile? image);
        Task<DoctorDeleteResultDTO> DeleteDoctorAsync(string id);
        Task<DoctorToggleAvailabilityResultDTO> ToggleAvailabilityAsync(string id);
    }
}
