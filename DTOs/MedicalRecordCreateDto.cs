
using System.ComponentModel.DataAnnotations;

namespace Healthcare.API.DTOs
{
    public class MedicalRecordCreateDto
    {
        [Required]
        public int PatientId { get; set; }

        [Required]
        public DateTime RecordDate { get; set; }

        [Required]
        [StringLength(200)]
        public string Diagnosis { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Treatment { get; set; }

        public string? Notes { get; set; }
    }
}
