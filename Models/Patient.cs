using System.ComponentModel.DataAnnotations;

namespace Healthcare.API.Models
{
    public class Patient
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public DateOnly DateOfBirth { get; set; }

        [Phone]
        public string? PhoneNumber { get; set; }
    }
}