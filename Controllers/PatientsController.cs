
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

        public PatientsController(
            IPatientService patientService,
            HealthcareDbContext context)
        {
            _patientService = patientService;
            _context = context;
        }

        // Admin: View all patients
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult GetAllPatients()
        {
            var patients = _patientService.GetAllPatients();
            return Ok(patients);
        }

        // Admin: View any patient
        // Patient: View only their own profile
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Patient")]
        public async Task<IActionResult> GetPatientById(int id)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p =>
                    p.Id == id &&
                    (User.IsInRole("Admin") ||
                     p.ApplicationUserId == userId));

            if (patient == null)
                return NotFound();

            return Ok(patient);
        }

        // Admin: Create patient profile
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult CreatePatient(PatientCreateDto patientDto)
        {
            var patient = _patientService.CreatePatient(patientDto);

            return CreatedAtAction(
                nameof(GetPatientById),
                new { id = patient.Id },
                patient
            );
        }

        // Admin: Update any patient
        // Patient: Update only their own profile
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Patient")]
        public async Task<IActionResult> UpdatePatient(
            int id,
            PatientCreateDto patientDto)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p =>
                    p.Id == id &&
                    (User.IsInRole("Admin") ||
                     p.ApplicationUserId == userId));

            if (patient == null)
                return NotFound();

            // Update only editable profile fields.
            patient.FirstName = patientDto.FirstName;
            patient.LastName = patientDto.LastName;
            patient.Email = patientDto.Email;
            patient.DateOfBirth = patientDto.DateOfBirth;
            patient.PhoneNumber = patientDto.PhoneNumber;

            await _context.SaveChangesAsync();

            return Ok("Patient updated successfully");
        }

        // Admin only: Delete patient profile
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult DeletePatient(int id)
        {
            var deleted = _patientService.DeletePatient(id);

            if (!deleted)
                return NotFound();

            return Ok("Patient deleted successfully");
        }

        // Patient: Get their own profile without needing to know their ID
        [HttpGet("my-profile")]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p =>
                    p.ApplicationUserId == userId);

            if (patient == null)
                return NotFound(new
                {
                    message = "Patient profile not found."
                });

            return Ok(patient);
        }
    }
}
