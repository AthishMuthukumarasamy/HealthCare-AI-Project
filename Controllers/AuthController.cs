using Healthcare.API.DTOs;
using Healthcare.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Healthcare.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AuthController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);

            if (existingUser != null)
            {
                return BadRequest(new
                {
                    message = "Email is already registered."
                });
            }

            var user = new ApplicationUser
            {
                FullName = dto.FullName,
                Email = dto.Email,
                UserName = dto.Email
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            // Public registration always assigns the Patient role.
            var roleResult = await _userManager.AddToRoleAsync(user, "Patient");

            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);

                return StatusCode(500, new
                {
                    message = "Unable to assign the Patient role."
                });
            }

            return Ok(new
            {
                message = "Patient registered successfully.",
                userId = user.Id,
                fullName = user.FullName,
                email = user.Email,
                role = "Patient"
            });
        }
    }
}