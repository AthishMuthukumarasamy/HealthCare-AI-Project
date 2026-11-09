
using System.ComponentModel.DataAnnotations;

namespace Healthcare.API.DTOs
{
    public class AppointmentCreateDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Patient ID must be greater than zero.")]
        public int PatientId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Provider ID must be greater than zero.")]
        public int ProviderId { get; set; }

        [Required]
        public DateTime AppointmentDate { get; set; }

        [Required]
        [StringLength(500, MinimumLength = 3,
            ErrorMessage = "Reason must be between 3 and 500 characters.")]
        public string Reason { get; set; } = string.Empty;
    }
}
