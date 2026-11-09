
using Healthcare.API.DTOs;
using Healthcare.API.Models;
using Healthcare.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Healthcare.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class DoctorsController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly HealthcareDbContext _context;

        public DoctorsController(
            UserManager<ApplicationUser> userManager,
            HealthcareDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterDoctor(
            CreateDoctorDto dto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var normalizedEmail =
                _userManager.NormalizeEmail(dto.Email.Trim());

            var existingUser = await _userManager
                .FindByEmailAsync(dto.Email.Trim());

            if (existingUser != null)
            {
                return BadRequest(new
                {
                    message = "Email is already registered."
                });
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var doctor = new ApplicationUser
                {
                    FullName = dto.FullName.Trim(),
                    Email = dto.Email.Trim(),
                    UserName = dto.Email.Trim(),
                    NormalizedEmail = normalizedEmail,
                    NormalizedUserName =
                        _userManager.NormalizeName(dto.Email.Trim())
                };

                var result = await _userManager.CreateAsync(
                    doctor, dto.Password);

                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync();

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
                    await transaction.RollbackAsync();

                    return StatusCode(500, new
                    {
                        message = "Unable to assign Doctor role."
                    });
                }

                var provider = new Provider
                {
                    FirstName = dto.FirstName.Trim(),
                    LastName = dto.LastName.Trim(),
                    Specialization = dto.Specialization.Trim(),
                    Email = dto.Email.Trim(),
                    PhoneNumber = dto.PhoneNumber?.Trim(),
                    YearsOfExperience = dto.YearsOfExperience,
                    ApplicationUserId = doctor.Id
                };

                _context.Providers.Add(provider);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Doctor registered successfully.",
                    userId = doctor.Id,
                    providerId = provider.Id,
                    fullName = doctor.FullName,
                    email = doctor.Email,
                    role = "Doctor",
                    specialization = provider.Specialization
                });
            }
            catch
            {
                await transaction.RollbackAsync();

                return StatusCode(500, new
                {
                    message = "Doctor registration failed. No changes were saved."
                });
            }
        }
    }
}
