
using Healthcare.API.DTOs;

namespace Healthcare.API.Services
{
    public interface IMedicalRecordService
    {
        List<MedicalRecordResponseDto> GetAllMedicalRecords();

        MedicalRecordResponseDto? GetMedicalRecordById(int id);

        List<MedicalRecordResponseDto> GetMedicalRecordsByPatientId(int patientId);

        MedicalRecordResponseDto CreateMedicalRecord(MedicalRecordCreateDto recordDto);

        bool UpdateMedicalRecord(int id, MedicalRecordCreateDto recordDto);

        bool DeleteMedicalRecord(int id);
    }
}
