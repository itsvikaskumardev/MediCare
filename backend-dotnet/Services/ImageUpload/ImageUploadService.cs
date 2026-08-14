using backend_dotnet.Models;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using static System.Net.Mime.MediaTypeNames;

namespace backend_dotnet.Services.ImageUpload
{
    public class ImageUploadService : IImageUploadService
    {
        private readonly Cloudinary _cloudinary;

        public ImageUploadService(IOptions<CloudinarySettings> config)
        {
            var account = new Account(
                config.Value.CloudName,
                config.Value.ApiKey,
                config.Value.ApiSecret
            );

            _cloudinary = new Cloudinary(account);
        }

        //-----------------------------------UploadImageAsync--------------------------------------------


        public async Task<string?> UploadImageAsync(IFormFile file, string folderName = "medicare") // IFormFile represents the uploaded file,
        {
            /*
             For example, the frontend might send: doctor.jpg , and your backend receives it as an IFormFile.
             */
            if (file == null || file.Length == 0)
            {
                Console.WriteLine("ImageUploadService: File is null or empty.");
                return null;
            }

            Console.WriteLine($"ImageUploadService: Attempting to upload image {file.FileName} to Cloudinary (folder: {folderName})...");

            await using var stream = file.OpenReadStream();// Instead of loading the entire image into a byte[], you're opening a stream to read the image data.
            //Stream containing image data
            var uploadParams = new ImageUploadParams // This object tells Cloudinary what to upload and how to handle it.
            {
                File = new FileDescription(file.FileName, stream),// Here is the file named doctor.jpg, and its actual image data is available through this stream."
                Folder = folderName,
                Transformation = new Transformation().Quality("auto").FetchFormat("auto")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);// This is the actual upload. Your application sends the image data to Cloudinary.

            Console.WriteLine($"ImageUploadService: Cloudinary response status: {uploadResult.StatusCode}");
            /*
             Cloudinary then returns a response containing information about the uploaded image.

              For example, it can contain: StatusCode, SecureUrl, PublicId, Error
             */

            if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
            {
                Console.WriteLine($"ImageUploadService: Upload successful! URL: {uploadResult.SecureUrl}");
                return uploadResult.SecureUrl.ToString();
            }

            Console.WriteLine($"ImageUploadService: Upload failed! Error: {uploadResult.Error?.Message}");
            return null;
        }

        //-----------------------------------DeleteImageAsync--------------------------------------------


        public async Task<bool> DeleteImageAsync(string publicId)
        {
            if (string.IsNullOrEmpty(publicId))
                return false;

            var deleteParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deleteParams);

            return result.Result == "ok";
        }
    }
}
