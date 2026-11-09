
using System.ComponentModel.DataAnnotations;

namespace Healthcare.API.Models
{
    public class MedicalRecord
    {
        public int Id { get; set; }

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

        // Audit properties
        [Required]
        public string CreatedBy { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; set; }

        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        public Patient? Patient { get; set; }
    }
}
