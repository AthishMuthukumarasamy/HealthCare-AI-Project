
using Healthcare.API.DTOs;
using Healthcare.API.Models;

namespace Healthcare.API.Services
{
    public class PatientService : IPatientService
    {
        private static readonly List<Patient> patients = new()
        {
            new Patient
            {
                Id = 1,
                FirstName = "Arun",
                LastName = "Kumar",
                Email = "arun@example.com",
                DateOfBirth = new DateOnly(1995, 5, 15),
                PhoneNumber = "9876543210"
            },
            new Patient
            {
                Id = 2,
                FirstName = "Priya",
                LastName = "Sharma",
                Email = "priya@example.com",
                DateOfBirth = new DateOnly(1998, 8, 20),
                PhoneNumber = "9876543211"
            }
        };

        public List<PatientResponseDto> GetAllPatients()
        {
            return patients.Select(patient => new PatientResponseDto
            {
                Id = patient.Id,
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                Email = patient.Email,
                DateOfBirth = patient.DateOfBirth
            }).ToList();
        }

        public PatientResponseDto? GetPatientById(int id)
        {
            var patient = patients.FirstOrDefault(p => p.Id == id);

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
                Id = patients.Count == 0 ? 1 : patients.Max(p => p.Id) + 1,
                FirstName = patientDto.FirstName,
                LastName = patientDto.LastName,
                Email = patientDto.Email,
                DateOfBirth = patientDto.DateOfBirth,
                PhoneNumber = patientDto.PhoneNumber
            };

            patients.Add(patient);

            return new PatientResponseDto
            {
                Id = patient.Id,
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                Email = patient.Email,
                DateOfBirth = patient.DateOfBirth
            };
        }

        public bool UpdatePatient(int id, PatientCreateDto patientDto)
        {
            var patient = patients.FirstOrDefault(p => p.Id == id);

            if (patient == null)
                return false;

            patient.FirstName = patientDto.FirstName;
            patient.LastName = patientDto.LastName;
            patient.Email = patientDto.Email;
            patient.DateOfBirth = patientDto.DateOfBirth;
            patient.PhoneNumber = patientDto.PhoneNumber;

            return true;
        }

        public bool DeletePatient(int id)
        {
            var patient = patients.FirstOrDefault(p => p.Id == id);

            if (patient == null)
                return false;

            patients.Remove(patient);

            return true;
        }
    }
}
