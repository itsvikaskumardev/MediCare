using backend_dotnet.Models.DTOs.Doctor;

namespace backend_dotnet.Services.Doctor
{
    public interface IDoctorService
    {
        Task<DoctorAuthResultDTO> CreateDoctorAsync(CreateDoctorRequestDTO request, IFormFile? image);
        Task<DoctorAuthResultDTO> LoginDoctorAsync(DoctorLoginRequestDTO request);
    }
}
