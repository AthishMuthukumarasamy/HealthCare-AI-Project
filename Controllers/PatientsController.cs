
using Healthcare.API.Data;
using Healthcare.API.DTOs;
using Healthcare.API.Models;
using Healthcare.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Healthcare.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PatientsController : ControllerBase
    {
        private readonly IPatientService _patientService;
        private readonly HealthcareDbContext _context;
        private readonly ILogger<PatientsController> _logger;

        public PatientsController(
            IPatientService patientService,
            HealthcareDbContext context,
            ILogger<PatientsController> logger)
        {
            _patientService = patientService;
            _context = context;
            _logger = logger;
        }

        // Admin: View all patients with pagination
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllPatients(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new
                {
                    message = "Page number must be at least 1 and page size must be between 1 and 100."
                });
            }

            var query = _context.Patients
                .AsNoTracking()
                .OrderBy(p => p.Id);

            var totalRecords = await query.CountAsync();

            var patients = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PatientResponseDto
                {
                    Id = p.Id,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    Email = p.Email,
                    DateOfBirth = p.DateOfBirth
                })
                .ToListAsync();

            var response = new PagedResponse<PatientResponseDto>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(
                    (double)totalRecords / pageSize),
                Data = patients
            };

            return Ok(response);
        }

        // Admin: View any patient
        // Patient: View only their own profile
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin,Patient")]
        public async Task<IActionResult> GetPatientById(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    message = "Invalid patient ID."
                });
            }

            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            var patient = await _context.Patients
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.Id == id &&
                    (User.IsInRole("Admin") ||
                     p.ApplicationUserId == userId));

            if (patient == null)
            {
                return NotFound();
            }

            return Ok(MapToResponseDto(patient));
        }

        // Admin: Create patient profile
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult CreatePatient(
            PatientCreateDto patientDto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var patient = _patientService.CreatePatient(patientDto);

            return CreatedAtAction(
                nameof(GetPatientById),
                new { id = patient.Id },
                patient
            );
        }

        // Admin: Update any patient
        // Patient: Update only their own profile
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Patient")]
        public async Task<IActionResult> UpdatePatient(
            int id,
            PatientUpdateDto patientDto)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    message = "Invalid patient ID."
                });
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            if (patientDto.DateOfBirth == default ||
                patientDto.DateOfBirth >
                DateOnly.FromDateTime(DateTime.Today))
            {
                return BadRequest(new
                {
                    message = "Please provide a valid date of birth."
                });
            }

            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p =>
                    p.Id == id &&
                    (User.IsInRole("Admin") ||
                     p.ApplicationUserId == userId));

            if (patient == null)
            {
                return NotFound();
            }

            // Update editable profile fields.
            patient.FirstName = patientDto.FirstName.Trim();
            patient.LastName = patientDto.LastName.Trim();
            patient.DateOfBirth = patientDto.DateOfBirth;
            patient.PhoneNumber = patientDto.PhoneNumber?.Trim();

            // Email changes are handled separately through
            // a verified Identity email-change workflow.

            try
            {
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Patient profile updated successfully.",
                    patient = MapToResponseDto(patient)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to update patient profile {PatientId}.",
                    id);

                return StatusCode(500, new
                {
                    message = "An error occurred while updating the patient profile."
                });
            }
        }

        // Admin only: Delete patient profile
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public IActionResult DeletePatient(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    message = "Invalid patient ID."
                });
            }

            var deleted = _patientService.DeletePatient(id);

            if (!deleted)
            {
                return NotFound();
            }

            return Ok("Patient deleted successfully.");
        }

        // Patient: Get their own profile
        [HttpGet("my-profile")]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            var patient = await _context.Patients
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.ApplicationUserId == userId);

            if (patient == null)
            {
                return NotFound(new
                {
                    message = "Patient profile not found."
                });
            }

            return Ok(MapToResponseDto(patient));
        }

        // Map Patient entity to safe response DTO
        private static PatientResponseDto MapToResponseDto(
            Patient patient)
        {
            return new PatientResponseDto
            {
                Id = patient.Id,
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                Email = patient.Email,
                DateOfBirth = patient.DateOfBirth
            };
        }
    }
}
