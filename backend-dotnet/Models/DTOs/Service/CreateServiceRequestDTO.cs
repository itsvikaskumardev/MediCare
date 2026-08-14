namespace backend_dotnet.Models.DTOs.Service
{
    public class CreateServiceRequestDTO
    {
        public string Name { get; set; } = string.Empty;
        public string? About { get; set; }
        public string? ShortDescription { get; set; }
        public string? Price { get; set; }          // raw, sanitized in service
        public string? Availability { get; set; }   // raw, parsed in service
        public string? Instructions { get; set; }   // JSON array as string (from form)
        public string? Slots { get; set; }          // JSON array as string (from form)
        public Microsoft.AspNetCore.Http.IFormFile? Image { get; set; }
    }
}
