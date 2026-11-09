namespace Healthcare.API.DTOs
{
    public class MedicalRecordResponseDto
    {
        public int Id { get; set; }

        public int PatientId { get; set; }

        public string PatientName { get; set; } = string.Empty;

        public DateTime RecordDate { get; set; }

        public string Diagnosis { get; set; } = string.Empty;

        public string? Treatment { get; set; }

        public string? Notes { get; set; }

        // Audit properties
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}