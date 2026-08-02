namespace backend_dotnet.Models.DTOs.Doctor
{
    public class DoctorListResultDTO
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public List<DoctorListItemDTO> Data { get; set; } = [];
        public int Page { get; set; }
        public int Limit { get; set; }
        public int Total { get; set; }
    }
}
