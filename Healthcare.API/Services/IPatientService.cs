
using Healthcare.API.DTOs;

namespace Healthcare.API.Services
{
    public interface IPatientService
    {
        List<PatientResponseDto> GetAllPatients();

        PatientResponseDto? GetPatientById(int id);

        PatientResponseDto CreatePatient(PatientCreateDto patientDto);

        bool UpdatePatient(int id, PatientUpdateDto patientDto);

        bool DeletePatient(int id);
    }
}
