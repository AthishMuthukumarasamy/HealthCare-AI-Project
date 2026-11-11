
using Healthcare.API.Data;
using Healthcare.API.DTOs;
using Healthcare.API.Models;
using Microsoft.Extensions.Logging;

namespace Healthcare.API.Services
{
    public class PatientService : IPatientService
    {
        private readonly HealthcareDbContext _context;
        private readonly ILogger<PatientService> _logger;

        public PatientService(
            HealthcareDbContext context,
            ILogger<PatientService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<PatientResponseDto> GetAllPatients()
        {
            _logger.LogInformation(
                "Retrieving all patients.");

            var patients = _context.Patients
                .Select(patient => new PatientResponseDto
                {
                    Id = patient.Id,
                    FirstName = patient.FirstName,
                    LastName = patient.LastName,
                    Email = patient.Email,
                    DateOfBirth = patient.DateOfBirth
                })
                .ToList();

            _logger.LogInformation(
                "Successfully retrieved {PatientCount} patients.",
                patients.Count);

            return patients;
        }

        public PatientResponseDto? GetPatientById(int id)
        {
            _logger.LogInformation(
                "Retrieving patient with ID {PatientId}.", id);

            var patient = _context.Patients
                .FirstOrDefault(p => p.Id == id);

            if (patient == null)
            {
                _logger.LogWarning(
                    "Patient with ID {PatientId} was not found.", id);

                return null;
            }

            _logger.LogInformation(
                "Successfully retrieved patient with ID {PatientId}.",
                id);

            return new PatientResponseDto
            {
                Id = patient.Id,
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                Email = patient.Email,
                DateOfBirth = patient.DateOfBirth
            };
        }

        public PatientResponseDto CreatePatient(
            PatientCreateDto patientDto)
        {
            _logger.LogInformation(
                "Creating a new patient.");

            var patient = new Patient
            {
                FirstName = patientDto.FirstName,
                LastName = patientDto.LastName,
                Email = patientDto.Email,
                DateOfBirth = patientDto.DateOfBirth,
                PhoneNumber = patientDto.PhoneNumber
            };

            _context.Patients.Add(patient);
            _context.SaveChanges();

            _logger.LogInformation(
                "Patient created successfully with ID {PatientId}.",
                patient.Id);

            return new PatientResponseDto
            {
                Id = patient.Id,
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                Email = patient.Email,
                DateOfBirth = patient.DateOfBirth
            };
        }

        public bool UpdatePatient(
            int id,
            PatientUpdateDto patientDto)
        {
            _logger.LogInformation(
                "Updating patient with ID {PatientId}.", id);

            var patient = _context.Patients
                .FirstOrDefault(p => p.Id == id);

            if (patient == null)
            {
                _logger.LogWarning(
                    "Cannot update patient. Patient with ID {PatientId} was not found.",
                    id);

                return false;
            }

            patient.FirstName = patientDto.FirstName;
            patient.LastName = patientDto.LastName;
            patient.DateOfBirth = patientDto.DateOfBirth;
            patient.PhoneNumber = patientDto.PhoneNumber;

            // Email is intentionally not updated here.
            // Email changes should use a verified Identity email-change flow.

            _context.SaveChanges();

            _logger.LogInformation(
                "Patient with ID {PatientId} updated successfully.",
                id);

            return true;
        }

        public bool DeletePatient(int id)
        {
            _logger.LogInformation(
                "Deleting patient with ID {PatientId}.", id);

            var patient = _context.Patients
                .FirstOrDefault(p => p.Id == id);

            if (patient == null)
            {
                _logger.LogWarning(
                    "Cannot delete patient. Patient with ID {PatientId} was not found.",
                    id);

                return false;
            }

            _context.Patients.Remove(patient);
            _context.SaveChanges();

            _logger.LogInformation(
                "Patient with ID {PatientId} deleted successfully.",
                id);

            return true;
        }
    }
}
