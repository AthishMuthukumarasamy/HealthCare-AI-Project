
using Healthcare.API.Data;
using Healthcare.API.DTOs;
using Healthcare.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Healthcare.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly HealthcareDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            HealthcareDbContext context,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _context = context;
            _configuration = configuration;
        }

        // Patient Registration
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var existingUser =
                await _userManager.FindByEmailAsync(dto.Email);

            if (existingUser != null)
            {
                return BadRequest(new
                {
                    message = "Email is already registered."
                });
            }

            // Validate that the date of birth is provided
            // and is not in the future.
            if (dto.DateOfBirth == default ||
                dto.DateOfBirth > DateOnly.FromDateTime(DateTime.Today))
            {
                return BadRequest(new
                {
                    message = "Please provide a valid date of birth."
                });
            }

            // Start a transaction so both records are saved together.
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var user = new ApplicationUser
                {
                    FullName = dto.FullName,
                    Email = dto.Email,
                    UserName = dto.Email
                };

                // Identity securely hashes the password.
                var result = await _userManager.CreateAsync(
                    user, dto.Password);

                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync();

                    return BadRequest(new
                    {
                        errors = result.Errors.Select(
                            e => e.Description)
                    });
                }

                // Public registration always assigns Patient role.
                var roleResult = await _userManager.AddToRoleAsync(
                    user, "Patient");

                if (!roleResult.Succeeded)
                {
                    await transaction.RollbackAsync();

                    return StatusCode(500, new
                    {
                        message = "Unable to assign the Patient role.",
                        errors = roleResult.Errors.Select(
                            e => e.Description)
                    });
                }

                // Create the Patient profile and link it
                // to the newly created Identity account.
                var patient = new Patient
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Email = dto.Email,
                    DateOfBirth = dto.DateOfBirth,
                    PhoneNumber = dto.PhoneNumber,
                    ApplicationUserId = user.Id
                };

                _context.Patients.Add(patient);

                await _context.SaveChangesAsync();

                // Commit both Identity and Patient records.
                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Patient registered successfully.",
                    userId = user.Id,
                    patientId = patient.Id,
                    fullName = user.FullName,
                    email = user.Email,
                    role = "Patient"
                });
            }
            catch
            {
                await transaction.RollbackAsync();

                return StatusCode(500, new
                {
                    message = "An error occurred during patient registration."
                });
            }
        }

        // User Login
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(
                dto.Email);

            if (user == null ||
                !await _userManager.CheckPasswordAsync(
                    user, dto.Password))
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            // Retrieve the user's assigned roles.
            var roles = await _userManager.GetRolesAsync(user);

            // Create JWT claims.
            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier, user.Id),

                new Claim(
                    ClaimTypes.Name, user.FullName),

                new Claim(
                    ClaimTypes.Email, user.Email!)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(
                    ClaimTypes.Role, role));
            }

            // Get JWT settings.
            var jwtKey = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException(
                    "JWT key is missing.");

            var jwtIssuer = _configuration["Jwt:Issuer"];
            var jwtAudience = _configuration["Jwt:Audience"];

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey));

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            // Set token expiration.
            var duration = double.Parse(
                _configuration["Jwt:DurationInMinutes"] ?? "30");

            var expiration = DateTime.UtcNow.AddMinutes(duration);

            // Generate JWT token.
            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: expiration,
                signingCredentials: credentials
            );

            var tokenString =
                new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new
            {
                message = "Login successful.",
                token = tokenString,
                expiration = expiration,
                userId = user.Id,
                fullName = user.FullName,
                email = user.Email,
                roles = roles
            });
        }

        // Authenticated User Profile
        [Authorize]
        [HttpGet("profile")]
        public IActionResult GetProfile()
        {
            return Ok(new
            {
                message = "You are authenticated!",
                userId = User.FindFirst(
                    ClaimTypes.NameIdentifier)?.Value,

                name = User.Identity?.Name,

                email = User.FindFirst(
                    ClaimTypes.Email)?.Value,

                roles = User.FindAll(ClaimTypes.Role)
                    .Select(r => r.Value)
            });
        }
    }
}
