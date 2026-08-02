namespace backend_dotnet.Models.DTOs.Doctor
{
    public class GetDoctorsRequestDTO
    {
        public string? Q { get; set; } = "";
        public int? Limit { get; set; } = 200;
        public int? Page { get; set; } = 1;
    }
}
