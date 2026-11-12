
using System.ComponentModel.DataAnnotations;

namespace Healthcare.API.DTOs
{
    public class LoginDto
    {
        [Required]
        [EmailAddress]
        [StringLength(254)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(128, MinimumLength = 1)]
        public string Password { get; set; } = string.Empty;
    }
}
