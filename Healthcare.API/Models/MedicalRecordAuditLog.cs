using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Healthcare.API.Models
{
    public class MedicalRecordAuditLog
    {
        public int Id { get; set; }

        public int? MedicalRecordId { get; set; }

        [Required]
        public int OriginalMedicalRecordId { get; set; }

        [Required]
        [StringLength(50)]
        public string Action { get; set; } = string.Empty;

        [Required]
        public string PerformedBy { get; set; } = string.Empty;

        [Required]
        public DateTime PerformedAt { get; set; }

        public string? Changes { get; set; }

        [ForeignKey(nameof(MedicalRecordId))]
        public MedicalRecord? MedicalRecord { get; set; }
    }
}