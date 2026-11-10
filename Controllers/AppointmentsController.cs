
using System.Security.Claims;
using Healthcare.API.Data;
using Healthcare.API.DTOs;
using Healthcare.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Healthcare.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AppointmentsController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;
        private readonly HealthcareDbContext _context;

        public AppointmentsController(
            IAppointmentService appointmentService,
            HealthcareDbContext context)
        {
            _appointmentService = appointmentService;
            _context = context;
        }

        // Admin only: View all appointments with pagination
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllAppointments(
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

            var query = _context.Appointments
                .AsNoTracking()
                .OrderBy(a => a.Id);

            var totalRecords = await query.CountAsync();

            var appointments = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AppointmentResponseDto
                {
                    Id = a.Id,
                    PatientId = a.PatientId,
                    PatientName = a.Patient != null
                        ? a.Patient.FirstName + " " + a.Patient.LastName
                        : string.Empty,
                    ProviderId = a.ProviderId,
                    ProviderName = a.Provider != null
                        ? a.Provider.FirstName + " " + a.Provider.LastName
                        : string.Empty,
                    AppointmentDate = a.AppointmentDate,
                    Reason = a.Reason,
                    Status = a.Status
                })
                .ToListAsync();

            var response = new PagedResponse<AppointmentResponseDto>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(
                    (double)totalRecords / pageSize),
                Data = appointments
            };

            return Ok(response);
        }

        // Admin, Patient or Doctor: View their appointments with pagination
        [HttpGet("my")]
        [Authorize(Roles = "Admin,Patient,Doctor")]
        public async Task<IActionResult> GetMyAppointments(
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

            var query = _context.Appointments
                .AsNoTracking();

            // Patient: Only their own appointments
            if (User.IsInRole("Patient"))
            {
                var userId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var patient = await _context.Patients
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        p => p.ApplicationUserId == userId);

                if (patient == null)
                    return NotFound();

                query = query.Where(
                    a => a.PatientId == patient.Id);
            }
            // Doctor: Only appointments assigned to them
            else if (User.IsInRole("Doctor"))
            {
                var userId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var provider = await _context.Providers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        p => p.ApplicationUserId == userId);

                if (provider == null)
                    return NotFound();

                query = query.Where(
                    a => a.ProviderId == provider.Id);
            }
            // Admin: No filter, so all appointments are visible
            else if (!User.IsInRole("Admin"))
            {
                return Forbid();
            }

            query = query.OrderBy(a => a.Id);

            var totalRecords = await query.CountAsync();

            var appointments = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AppointmentResponseDto
                {
                    Id = a.Id,
                    PatientId = a.PatientId,
                    PatientName = a.Patient != null
                        ? a.Patient.FirstName + " " + a.Patient.LastName
                        : string.Empty,
                    ProviderId = a.ProviderId,
                    ProviderName = a.Provider != null
                        ? a.Provider.FirstName + " " + a.Provider.LastName
                        : string.Empty,
                    AppointmentDate = a.AppointmentDate,
                    Reason = a.Reason,
                    Status = a.Status
                })
                .ToListAsync();

            var response = new PagedResponse<AppointmentResponseDto>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(
                    (double)totalRecords / pageSize),
                Data = appointments
            };

            return Ok(response);
        }

        // Admin, Patient or Doctor: View one appointment if authorized
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Patient,Doctor")]
        public async Task<IActionResult> GetAppointmentById(int id)
        {
            var appointment = _appointmentService.GetAppointmentById(id);

            if (appointment == null)
                return NotFound();

            if (User.IsInRole("Admin"))
                return Ok(appointment);

            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (User.IsInRole("Patient"))
            {
                var hasAccess = await _context.Patients.AnyAsync(
                    p => p.Id == appointment.PatientId
                    && p.ApplicationUserId == userId);

                if (!hasAccess)
                    return NotFound();
            }
            else if (User.IsInRole("Doctor"))
            {
                var hasAccess = await _context.Providers.AnyAsync(
                    p => p.Id == appointment.ProviderId
                    && p.ApplicationUserId == userId);

                if (!hasAccess)
                    return NotFound();
            }
            else
            {
                return Forbid();
            }

            return Ok(appointment);
        }

        // Admin only: Create appointment
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult CreateAppointment(
            AppointmentCreateDto appointmentDto)
        {
            try
            {
                var appointment = _appointmentService
                    .CreateAppointment(appointmentDto);

                return CreatedAtAction(
                    nameof(GetAppointmentById),
                    new { id = appointment.Id },
                    appointment);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Admin only: Update appointment details
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult UpdateAppointment(
            int id,
            AppointmentCreateDto appointmentDto)
        {
            try
            {
                var updated = _appointmentService
                    .UpdateAppointment(id, appointmentDto);

                if (!updated)
                    return NotFound();

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Admin only: Update appointment status
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public IActionResult UpdateAppointmentStatus(
            int id,
            AppointmentStatusUpdateDto statusDto)
        {
            try
            {
                var updated = _appointmentService
                    .UpdateAppointmentStatus(id, statusDto.Status);

                if (!updated)
                    return NotFound();

                return Ok(new
                {
                    message = "Appointment status updated successfully.",
                    status = statusDto.Status
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Admin only: Delete appointment
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteAppointment(int id)
        {
            var deleted = _appointmentService.DeleteAppointment(id);

            if (!deleted)
                return NotFound();

            return NoContent();
        }
    }
}
