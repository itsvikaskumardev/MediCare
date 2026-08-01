using System.Net;

namespace backend_dotnet.Models
{
    public class ApiResponse
    {
        public ApiResponse()
        {
            ErrorMessages = [];
        }

        public bool IsSuccess { get; set; } = true;

        public object? Result { get; set; }

        public HttpStatusCode StatusCode { get; set; }

        public List<string?> ErrorMessages { get; set; }
    }
}