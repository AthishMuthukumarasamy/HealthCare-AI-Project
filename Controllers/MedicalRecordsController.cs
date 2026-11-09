
using Healthcare.API.Data;
using Healthcare.API.DTOs;
using Healthcare.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Healthcare.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MedicalRecordsController : ControllerBase
    {
        private readonly IMedicalRecordService _medicalRecordService;
        private readonly HealthcareDbContext _context;

        public MedicalRecordsController(
            IMedicalRecordService medicalRecordService,
            HealthcareDbContext context)
        {
            _medicalRecordService = medicalRecordService;
            _context = context;
        }

        // Admin: View all medical records
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult GetAllMedicalRecords()
        {
            return Ok(_medicalRecordService.GetAllMedicalRecords());
        }

        // Admin: View any record
        // Patient: View own record
        // Doctor: View records of assigned patients
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Patient,Doctor")]
        public async Task<IActionResult> GetMedicalRecordById(int id)
        {
            var record = await _context.MedicalRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (record == null)
                return NotFound();

            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!User.IsInRole("Admin"))
            {
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var hasAccess = await HasRecordAccess(
                    record.PatientId, userId);

                if (!hasAccess)
                    return NotFound();
            }

            var result =
                _medicalRecordService.GetMedicalRecordById(id);

            return Ok(result);
        }

        // Admin: View any patient's records
        // Patient: View own records
        // Doctor: View records of assigned patients
        [HttpGet("patient/{patientId}")]
        [Authorize(Roles = "Admin,Patient,Doctor")]
        public async Task<IActionResult> GetMedicalRecordsByPatientId(
            int patientId)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!User.IsInRole("Admin"))
            {
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var hasAccess = await HasRecordAccess(
                    patientId, userId);

                if (!hasAccess)
                    return NotFound();
            }

            var records =
                _medicalRecordService.GetMedicalRecordsByPatientId(
                    patientId);

            return Ok(records);
        }

        // Admin only: Create medical record
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult CreateMedicalRecord(
            MedicalRecordCreateDto recordDto)
        {
            try
            {
                var userId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var record =
                    _medicalRecordService.CreateMedicalRecord(
                        recordDto, userId);

                return CreatedAtAction(
                    nameof(GetMedicalRecordById),
                    new { id = record.Id },
                    record);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Admin only: Update medical record
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult UpdateMedicalRecord(
            int id,
            MedicalRecordCreateDto recordDto)
        {
            try
            {
                var userId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var updated =
                    _medicalRecordService.UpdateMedicalRecord(
                        id, recordDto, userId);

                if (!updated)
                    return NotFound();

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Admin only: Delete medical record
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteMedicalRecord(int id)
        {
            var deleted =
                _medicalRecordService.DeleteMedicalRecord(id);

            if (!deleted)
                return NotFound();

            return NoContent();
        }

        // Checks whether the logged-in user can access
        // records belonging to the specified patient.
        private async Task<bool> HasRecordAccess(
            int patientId,
            string userId)
        {
            // Patient can access only their own records.
            if (User.IsInRole("Patient"))
            {
                return await _context.Patients.AnyAsync(p =>
                    p.Id == patientId &&
                    p.ApplicationUserId == userId);
            }

            // Doctor can access records when an appointment
            // exists with the patient, regardless of status.
            if (User.IsInRole("Doctor"))
            {
                return await _context.Appointments.AnyAsync(a =>
                    a.PatientId == patientId &&
                    a.Provider != null &&
                    a.Provider.ApplicationUserId == userId);
            }

            return false;
        }
    }
}
