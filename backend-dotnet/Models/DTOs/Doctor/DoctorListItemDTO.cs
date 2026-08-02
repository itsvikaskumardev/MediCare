namespace backend_dotnet.Models.DTOs.Doctor
{
    public class DoctorListItemDTO
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = "";
        public string Specialization { get; set; } = "";
        public decimal Fee { get; set; }
        public string? ImageUrl { get; set; }
        public int AppointmentsTotal { get; set; }
        public int AppointmentsCompleted { get; set; }
        public int AppointmentsCanceled { get; set; }
        public decimal Earnings { get; set; }
        public string Availability { get; set; } = "Available";
        public object Schedule { get; set; } = new { };
        public string Patients { get; set; } = "";
        public decimal Rating { get; set; }
        public string About { get; set; } = "";
        public string Experience { get; set; } = "";
        public string Qualifications { get; set; } = "";
        public string Location { get; set; } = "";
        public string Success { get; set; } = "";
    }
}
