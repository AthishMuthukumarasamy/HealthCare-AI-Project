
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

        // Admin: View all medical records with search, filtering,
        // sorting and pagination
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllMedicalRecords(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] int? patientId = null,
            [FromQuery] string sortBy = "id",
            [FromQuery] string sortOrder = "asc")
        {
            if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new
                {
                    message = "Page number must be at least 1 and page size must be between 1 and 100."
                });
            }

            if (patientId.HasValue && patientId.Value <= 0)
            {
                return BadRequest(new
                {
                    message = "Patient ID must be greater than 0."
                });
            }

            var allowedSortFields = new[]
            {
                "id",
                "recorddate",
                "diagnosis",
                "patientid",
                "createdat",
                "updatedat"
            };

            sortBy = sortBy.Trim().ToLowerInvariant();
            sortOrder = sortOrder.Trim().ToLowerInvariant();

            if (!allowedSortFields.Contains(sortBy))
            {
                return BadRequest(new
                {
                    message = "Invalid sortBy value. Allowed values: id, recordDate, diagnosis, patientId, createdAt, updatedAt."
                });
            }

            if (sortOrder != "asc" && sortOrder != "desc")
            {
                return BadRequest(new
                {
                    message = "sortOrder must be either 'asc' or 'desc'."
                });
            }

            var query = _context.MedicalRecords
                .AsNoTracking()
                .AsQueryable();

            // Filter by patient ID
            if (patientId.HasValue)
            {
                query = query.Where(m => m.PatientId == patientId.Value);
            }

            // Search diagnosis, treatment, notes and patient name
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = search.Trim();

                query = query.Where(m =>
                    m.Diagnosis.Contains(searchTerm) ||
                    (m.Treatment != null &&
                     m.Treatment.Contains(searchTerm)) ||
                    (m.Notes != null &&
                     m.Notes.Contains(searchTerm)) ||
                    (m.Patient != null &&
                     (m.Patient.FirstName.Contains(searchTerm) ||
                      m.Patient.LastName.Contains(searchTerm))));
            }

            // Apply sorting
            query = sortBy switch
            {
                "recorddate" => sortOrder == "asc"
                    ? query.OrderBy(m => m.RecordDate).ThenBy(m => m.Id)
                    : query.OrderByDescending(m => m.RecordDate).ThenBy(m => m.Id),

                "diagnosis" => sortOrder == "asc"
                    ? query.OrderBy(m => m.Diagnosis).ThenBy(m => m.Id)
                    : query.OrderByDescending(m => m.Diagnosis).ThenBy(m => m.Id),

                "patientid" => sortOrder == "asc"
                    ? query.OrderBy(m => m.PatientId).ThenBy(m => m.Id)
                    : query.OrderByDescending(m => m.PatientId).ThenBy(m => m.Id),

                "createdat" => sortOrder == "asc"
                    ? query.OrderBy(m => m.CreatedAt).ThenBy(m => m.Id)
                    : query.OrderByDescending(m => m.CreatedAt).ThenBy(m => m.Id),

                "updatedat" => sortOrder == "asc"
                    ? query.OrderBy(m => m.UpdatedAt).ThenBy(m => m.Id)
                    : query.OrderByDescending(m => m.UpdatedAt).ThenBy(m => m.Id),

                _ => sortOrder == "asc"
                    ? query.OrderBy(m => m.Id)
                    : query.OrderByDescending(m => m.Id)
            };

            var totalRecords = await query.CountAsync();

            var medicalRecords = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MedicalRecordResponseDto
                {
                    Id = m.Id,
                    PatientId = m.PatientId,
                    PatientName = m.Patient != null
                        ? m.Patient.FirstName + " " + m.Patient.LastName
                        : string.Empty,
                    RecordDate = m.RecordDate,
                    Diagnosis = m.Diagnosis,
                    Treatment = m.Treatment,
                    Notes = m.Notes,
                    CreatedBy = m.CreatedBy,
                    CreatedAt = m.CreatedAt,
                    UpdatedBy = m.UpdatedBy,
                    UpdatedAt = m.UpdatedAt
                })
                .ToListAsync();

            var response = new PagedResponse<MedicalRecordResponseDto>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(
                    (double)totalRecords / pageSize),
                Data = medicalRecords
            };

            return Ok(response);
        }

        // Admin only: View medical record audit history
        [HttpGet("{id}/audit-history")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetMedicalRecordAuditHistory(int id)
        {
            if (id <= 0)
                return BadRequest(new
                {
                    message = "Invalid medical record ID."
                });

            var auditHistory =
                _medicalRecordService.GetMedicalRecordAuditHistory(id);

            return Ok(auditHistory);
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

        // Admin only: Delete medical record with audit logging
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteMedicalRecord(int id)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var deleted =
                _medicalRecordService.DeleteMedicalRecord(
                    id, userId);

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
