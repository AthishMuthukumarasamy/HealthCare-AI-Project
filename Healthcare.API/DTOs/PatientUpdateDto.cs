
using System.ComponentModel.DataAnnotations;

namespace Healthcare.API.DTOs
{
    public class PatientUpdateDto
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        public DateOnly DateOfBirth { get; set; }

        [Phone]
        public string? PhoneNumber { get; set; }
    }
}
