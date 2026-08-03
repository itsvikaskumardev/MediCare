namespace backend_dotnet.Models.DTOs.Service
{
    public class UpdateServiceRequestDTO
    {
        public string? Name { get; set; }
        public string? About { get; set; }
        public string? ShortDescription { get; set; }
        public string? Price { get; set; }
        public string? Availability { get; set; }
        public string? Instructions { get; set; }
        public string? Slots { get; set; }
    }
}
