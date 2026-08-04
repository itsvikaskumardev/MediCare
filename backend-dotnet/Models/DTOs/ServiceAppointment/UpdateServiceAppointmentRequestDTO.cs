namespace backend_dotnet.Models.DTOs.ServiceAppointment
{
    public class UpdateServiceAppointmentRequestDTO
    {
        public string? Status { get; set; }
        public string? Notes { get; set; }
        public PaymentUpdateDTO? Payment { get; set; }
        public RescheduledToDTO? RescheduledTo { get; set; }
    }

    public class PaymentUpdateDTO
    {
        public string? Method { get; set; }
        public string? Status { get; set; }
        public decimal? Amount { get; set; }
        public DateTime? PaidAt { get; set; }
    }

    public class RescheduledToDTO
    {
        public string? Date { get; set; }
        public string? Time { get; set; }
    }


}
