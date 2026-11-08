
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

        // Admin only: View all appointments
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult GetAllAppointments()
        {
            return Ok(_appointmentService.GetAllAppointments());
        }

        // Admin, Patient or Doctor: View appointments assigned to them
        [HttpGet("my")]
        [Authorize(Roles = "Admin,Patient,Doctor")]
        public async Task<IActionResult> GetMyAppointments()
        {
            if (User.IsInRole("Admin"))
            {
                return Ok(_appointmentService.GetAllAppointments());
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (User.IsInRole("Patient"))
            {
                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.ApplicationUserId == userId);

                if (patient == null)
                    return NotFound();

                var appointments = _appointmentService
                    .GetAllAppointments()
                    .Where(a => a.PatientId == patient.Id);

                return Ok(appointments);
            }

            if (User.IsInRole("Doctor"))
            {
                var provider = await _context.Providers
                    .FirstOrDefaultAsync(p => p.ApplicationUserId == userId);

                if (provider == null)
                    return NotFound();

                var appointments = _appointmentService
                    .GetAllAppointments()
                    .Where(a => a.ProviderId == provider.Id);

                return Ok(appointments);
            }

            return Forbid();
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

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

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

        // Admin only: Update appointment
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
