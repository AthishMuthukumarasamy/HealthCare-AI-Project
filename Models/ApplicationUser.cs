using Microsoft.AspNetCore.Identity;

namespace Healthcare.API.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
    }
}