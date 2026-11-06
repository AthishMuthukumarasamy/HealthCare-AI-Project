
using System.ComponentModel.DataAnnotations;

namespace Healthcare.API.Models
{
    public class Appointment
    {
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }

        [Required]
        public int ProviderId { get; set; }

        [Required]
        public DateTime AppointmentDate { get; set; }

        [Required]
        public string Reason { get; set; } = string.Empty;

        [Required]
        public string Status { get; set; } = "Scheduled";

        // Navigation properties
        public Patient? Patient { get; set; }

        public Provider? Provider { get; set; }
    }
}
