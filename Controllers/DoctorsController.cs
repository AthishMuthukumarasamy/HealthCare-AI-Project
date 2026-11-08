
using Healthcare.API.DTOs;
using Healthcare.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Healthcare.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class DoctorsController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public DoctorsController(
            UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterDoctor(
            CreateDoctorDto dto)
        {
            var existingUser = await _userManager
                .FindByEmailAsync(dto.Email);

            if (existingUser != null)
            {
                return BadRequest(new
                {
                    message = "Email is already registered."
                });
            }

            var doctor = new ApplicationUser
            {
                FullName = dto.FullName,
                Email = dto.Email,
                UserName = dto.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(
                doctor, dto.Password);

            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    errors = result.Errors.Select(
                        e => e.Description)
                });
            }

            var roleResult = await _userManager.AddToRoleAsync(
                doctor, "Doctor");

            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(doctor);

                return StatusCode(500, new
                {
                    message = "Unable to assign Doctor role."
                });
            }

            return Ok(new
            {
                message = "Doctor registered successfully.",
                userId = doctor.Id,
                fullName = doctor.FullName,
                email = doctor.Email,
                role = "Doctor"
            });
        }
    }
}
