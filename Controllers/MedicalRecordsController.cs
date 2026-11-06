
using Healthcare.API.DTOs;
using Healthcare.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Healthcare.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MedicalRecordsController : ControllerBase
    {
        private readonly IMedicalRecordService _medicalRecordService;

        public MedicalRecordsController(IMedicalRecordService medicalRecordService)
        {
            _medicalRecordService = medicalRecordService;
        }

        [HttpGet]
        public IActionResult GetAllMedicalRecords()
        {
            return Ok(_medicalRecordService.GetAllMedicalRecords());
        }

        [HttpGet("{id}")]
        public IActionResult GetMedicalRecordById(int id)
        {
            var record = _medicalRecordService.GetMedicalRecordById(id);

            if (record == null)
                return NotFound();

            return Ok(record);
        }

        [HttpGet("patient/{patientId}")]
        public IActionResult GetMedicalRecordsByPatientId(int patientId)
        {
            var records = _medicalRecordService.GetMedicalRecordsByPatientId(patientId);

            return Ok(records);
        }

        [HttpPost]
        public IActionResult CreateMedicalRecord(MedicalRecordCreateDto recordDto)
        {
            try
            {
                var record = _medicalRecordService.CreateMedicalRecord(recordDto);

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

        [HttpPut("{id}")]
        public IActionResult UpdateMedicalRecord(int id, MedicalRecordCreateDto recordDto)
        {
            try
            {
                var updated = _medicalRecordService.UpdateMedicalRecord(id, recordDto);

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
        public IActionResult DeleteMedicalRecord(int id)
        {
            var deleted = _medicalRecordService.DeleteMedicalRecord(id);

            if (!deleted)
                return NotFound();

            return NoContent();
        }
    }
}
