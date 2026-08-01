using Microsoft.AspNetCore.Http;

namespace backend_dotnet.Services.ImageUpload
{
    public interface IImageUploadService
    {
        Task<string?> UploadImageAsync(IFormFile file, string folderName = "medicare");
        Task<bool> DeleteImageAsync(string publicId);
    }
}
