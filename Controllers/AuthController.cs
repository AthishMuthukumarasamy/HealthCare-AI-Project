
using Healthcare.API.Data;
using Healthcare.API.DTOs;
using Healthcare.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
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
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            HealthcareDbContext context,
            IConfiguration configuration,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        // Patient Registration
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var email = dto.Email.Trim();

            // Validate date of birth.
            if (dto.DateOfBirth == default ||
                dto.DateOfBirth > DateOnly.FromDateTime(DateTime.Today))
            {
                return BadRequest(new
                {
                    message = "Please provide a valid date of birth."
                });
            }

            var existingUser =
                await _userManager.FindByEmailAsync(email);

            if (existingUser != null)
            {
                return BadRequest(new
                {
                    message = "Email is already registered."
                });
            }

            // Start transaction for Identity and Patient records.
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var user = new ApplicationUser
                {
                    FullName = dto.FullName.Trim(),
                    Email = email,
                    UserName = email
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

                    _logger.LogError(
                        "Failed to assign Patient role during registration.");

                    return StatusCode(500, new
                    {
                        message = "Unable to assign the Patient role."
                    });
                }

                // Create and link Patient profile.
                var patient = new Patient
                {
                    FirstName = dto.FirstName.Trim(),
                    LastName = dto.LastName.Trim(),
                    Email = email,
                    DateOfBirth = dto.DateOfBirth,
                    PhoneNumber = dto.PhoneNumber?.Trim(),
                    ApplicationUserId = user.Id
                };

                _context.Patients.Add(patient);

                await _context.SaveChangesAsync();

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
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Patient registration failed for email {Email}.",
                    email);

                try
                {
                    await transaction.RollbackAsync();
                }
                catch (Exception rollbackException)
                {
                    _logger.LogError(
                        rollbackException,
                        "Failed to roll back patient registration transaction.");
                }

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
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var email = dto.Email.Trim();

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null ||
                !await _userManager.CheckPasswordAsync(
                    user, dto.Password))
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            // Retrieve assigned roles.
            var roles = await _userManager.GetRolesAsync(user);

            // Read JWT configuration.
            var jwtKey = _configuration["Jwt:Key"];
            var jwtIssuer = _configuration["Jwt:Issuer"];
            var jwtAudience = _configuration["Jwt:Audience"];

            if (string.IsNullOrWhiteSpace(jwtKey) ||
                string.IsNullOrWhiteSpace(jwtIssuer) ||
                string.IsNullOrWhiteSpace(jwtAudience) ||
                Encoding.UTF8.GetByteCount(jwtKey) < 32)
            {
                _logger.LogError(
                    "JWT configuration is missing or invalid.");

                return StatusCode(500, new
                {
                    message = "JWT configuration is invalid."
                });
            }

            // Validate token duration.
            var durationSetting =
                _configuration["Jwt:DurationInMinutes"];

            if (!double.TryParse(
                    durationSetting,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var duration) ||
                duration <= 0 ||
                duration > 1440)
            {
                _logger.LogError(
                    "JWT duration configuration is invalid.");

                return StatusCode(500, new
                {
                    message = "JWT duration configuration is invalid."
                });
            }

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

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey));

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            // Set token expiration.
            var expiration =
                DateTime.UtcNow.AddMinutes(duration);

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
