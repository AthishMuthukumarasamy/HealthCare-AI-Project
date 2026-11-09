
using Healthcare.API.DTOs;

namespace Healthcare.API.Services
{
    public interface IMedicalRecordService
    {
        List<MedicalRecordResponseDto> GetAllMedicalRecords();

        MedicalRecordResponseDto? GetMedicalRecordById(int id);

        List<MedicalRecordResponseDto> GetMedicalRecordsByPatientId(int patientId);

        MedicalRecordResponseDto CreateMedicalRecord(
            MedicalRecordCreateDto recordDto,
            string userId);

        bool UpdateMedicalRecord(
            int id,
            MedicalRecordCreateDto recordDto,
            string userId);

        bool DeleteMedicalRecord(int id);
    }
}
