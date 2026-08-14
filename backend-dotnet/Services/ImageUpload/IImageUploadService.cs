using Microsoft.AspNetCore.Http;

namespace backend_dotnet.Services.ImageUpload
{
    public interface IImageUploadService
    {
        Task<string?> UploadImageAsync(IFormFile file, string folderName = "medicare");
        Task<bool> DeleteImageAsync(string publicId);
    }
}

/**
 

-------------------------------------
Task<string?> UploadImageAsync(IFormFile file, string folderName = "medicare");


* `Task` → the method is **asynchronous**.
* `string` → when the async operation finishes, it returns a string.
* `?` → the string can be `null`.
----------------------

### 2. `IFormFile file`

This is an **input parameter**.

`IFormFile` represents a file uploaded through an HTTP request, commonly from a form/multipart request.

For example: var imageUrl = await UploadImageAsync(file);

Here `file` could be something like:

------------------------------------
 
 
 
 */
