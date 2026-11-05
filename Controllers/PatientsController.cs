
using Healthcare.API.DTOs;
using Healthcare.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Healthcare.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientsController : ControllerBase
    {
        private readonly IPatientService _patientService;

        public PatientsController(IPatientService patientService)
        {
            _patientService = patientService;
        }

        [HttpGet]
        public IActionResult GetAllPatients()
        {
            var patients = _patientService.GetAllPatients();
            return Ok(patients);
        }

        [HttpGet("{id}")]
        public IActionResult GetPatientById(int id)
        {
            var patient = _patientService.GetPatientById(id);

            if (patient == null)
                return NotFound();

            return Ok(patient);
        }

        [HttpPost]
        public IActionResult CreatePatient(PatientCreateDto patientDto)
        {
            var patient = _patientService.CreatePatient(patientDto);

            return CreatedAtAction(
                nameof(GetPatientById),
                new { id = patient.Id },
                patient
            );
        }

        [HttpPut("{id}")]
        public IActionResult UpdatePatient(int id, PatientCreateDto patientDto)
        {
            var updated = _patientService.UpdatePatient(id, patientDto);

            if (!updated)
                return NotFound();

            return Ok("Patient updated successfully");
        }

        [HttpDelete("{id}")]
        public IActionResult DeletePatient(int id)
        {
            var deleted = _patientService.DeletePatient(id);

            if (!deleted)
                return NotFound();

            return Ok("Patient deleted successfully");
        }
    }
}
