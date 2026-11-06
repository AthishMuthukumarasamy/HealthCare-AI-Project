
namespace Healthcare.API.DTOs
{
    public class AppointmentResponseDto
    {
        public int Id { get; set; }

        public int PatientId { get; set; }

        public string PatientName { get; set; } = string.Empty;

        public int ProviderId { get; set; }

        public string ProviderName { get; set; } = string.Empty;

        public DateTime AppointmentDate { get; set; }

        public string Reason { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
    }
}
