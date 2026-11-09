using System.ComponentModel.DataAnnotations;

namespace Healthcare.API.DTOs
{
    public class MedicalRecordCreateDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Patient ID must be greater than zero.")]
        public int PatientId { get; set; }

        [Required]
        public DateTime RecordDate { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string Diagnosis { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Treatment { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }
    }
}