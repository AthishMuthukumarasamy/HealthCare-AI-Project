
namespace Healthcare.API.DTOs
{
    public class MedicalRecordAuditLogResponseDto
    {
        public int Id { get; set; }

        public int? MedicalRecordId { get; set; }

        public int OriginalMedicalRecordId { get; set; }

        public string Action { get; set; } = string.Empty;

        public string PerformedBy { get; set; } = string.Empty;

        public DateTime PerformedAt { get; set; }

        public string? Changes { get; set; }
    }
}
