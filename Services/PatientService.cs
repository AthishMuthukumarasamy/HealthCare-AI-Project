
using Healthcare.API.Data;
using Healthcare.API.DTOs;
using Healthcare.API.Models;

namespace Healthcare.API.Services
{
    public class PatientService : IPatientService
    {
        private readonly HealthcareDbContext _context;

        public PatientService(HealthcareDbContext context)
        {
            _context = context;
        }

        public List<PatientResponseDto> GetAllPatients()
        {
            return _context.Patients
                .Select(patient => new PatientResponseDto
                {
                    Id = patient.Id,
                    FirstName = patient.FirstName,
                    LastName = patient.LastName,
                    Email = patient.Email,
                    DateOfBirth = patient.DateOfBirth
                })
                .ToList();
        }

        public PatientResponseDto? GetPatientById(int id)
        {
            var patient = _context.Patients
                .FirstOrDefault(p => p.Id == id);

            if (patient == null)
                return null;

            return new PatientResponseDto
            {
                Id = patient.Id,
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                Email = patient.Email,
                DateOfBirth = patient.DateOfBirth
            };
        }

        public PatientResponseDto CreatePatient(PatientCreateDto patientDto)
        {
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

            return new PatientResponseDto
            {
                Id = patient.Id,
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                Email = patient.Email,
                DateOfBirth = patient.DateOfBirth
            };
        }

        public bool UpdatePatient(int id, PatientUpdateDto patientDto)
        {
            var patient = _context.Patients
                .FirstOrDefault(p => p.Id == id);

            if (patient == null)
                return false;

            patient.FirstName = patientDto.FirstName;
            patient.LastName = patientDto.LastName;
            patient.DateOfBirth = patientDto.DateOfBirth;
            patient.PhoneNumber = patientDto.PhoneNumber;

            // Email is intentionally not updated here.
            // Email changes should use a verified Identity email-change flow.

            _context.SaveChanges();

            return true;
        }

        public bool DeletePatient(int id)
        {
            var patient = _context.Patients
                .FirstOrDefault(p => p.Id == id);

            if (patient == null)
                return false;

            _context.Patients.Remove(patient);
            _context.SaveChanges();

            return true;
        }
    }
}
