
using Healthcare.API.DTOs;
using Healthcare.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Healthcare.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentsController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;

        public AppointmentsController(IAppointmentService appointmentService)
        {
            _appointmentService = appointmentService;
        }

        [HttpGet]
        public IActionResult GetAllAppointments()
        {
            return Ok(_appointmentService.GetAllAppointments());
        }

        [HttpGet("{id}")]
        public IActionResult GetAppointmentById(int id)
        {
            var appointment = _appointmentService.GetAppointmentById(id);

            if (appointment == null)
                return NotFound();

            return Ok(appointment);
        }

        [HttpPost]
        public IActionResult CreateAppointment(AppointmentCreateDto appointmentDto)
        {
            try
            {
                var appointment = _appointmentService.CreateAppointment(appointmentDto);

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

        [HttpPut("{id}")]
        public IActionResult UpdateAppointment(int id, AppointmentCreateDto appointmentDto)
        {
            try
            {
                var updated = _appointmentService.UpdateAppointment(id, appointmentDto);

                if (!updated)
                    return NotFound();

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteAppointment(int id)
        {
            var deleted = _appointmentService.DeleteAppointment(id);

            if (!deleted)
                return NotFound();

            return NoContent();
        }
    }
}
