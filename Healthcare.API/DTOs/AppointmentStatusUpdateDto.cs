
using System.ComponentModel.DataAnnotations;

namespace Healthcare.API.DTOs
{
    public class AppointmentStatusUpdateDto
    {
        [Required]
        public string Status { get; set; } = string.Empty;
    }
}
