
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

        // Navigation property
        public Patient? Patient { get; set; }
    }
}
