
using Healthcare.API.Data;
using Healthcare.API.DTOs;
using Healthcare.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Healthcare.API.Services
{
    public class MedicalRecordService : IMedicalRecordService
    {
        private readonly HealthcareDbContext _context;

        public MedicalRecordService(HealthcareDbContext context)
        {
            _context = context;
        }

        public List<MedicalRecordResponseDto> GetAllMedicalRecords()
        {
            return _context.MedicalRecords
                .Include(m => m.Patient)
                .Select(m => new MedicalRecordResponseDto
                {
                    Id = m.Id,
                    PatientId = m.PatientId,
                    PatientName = m.Patient!.FirstName + " " + m.Patient.LastName,
                    RecordDate = m.RecordDate,
                    Diagnosis = m.Diagnosis,
                    Treatment = m.Treatment,
                    Notes = m.Notes,

                    CreatedBy = m.CreatedBy,
                    CreatedAt = m.CreatedAt,
                    UpdatedBy = m.UpdatedBy,
                    UpdatedAt = m.UpdatedAt
                })
                .ToList();
        }

        public MedicalRecordResponseDto? GetMedicalRecordById(int id)
        {
            var record = _context.MedicalRecords
                .Include(m => m.Patient)
                .FirstOrDefault(m => m.Id == id);

            if (record == null)
                return null;

            return new MedicalRecordResponseDto
            {
                Id = record.Id,
                PatientId = record.PatientId,
                PatientName = record.Patient!.FirstName + " " + record.Patient.LastName,
                RecordDate = record.RecordDate,
                Diagnosis = record.Diagnosis,
                Treatment = record.Treatment,
                Notes = record.Notes,

                CreatedBy = record.CreatedBy,
                CreatedAt = record.CreatedAt,
                UpdatedBy = record.UpdatedBy,
                UpdatedAt = record.UpdatedAt
            };
        }

        public List<MedicalRecordResponseDto> GetMedicalRecordsByPatientId(int patientId)
        {
            return _context.MedicalRecords
                .Include(m => m.Patient)
                .Where(m => m.PatientId == patientId)
                .Select(m => new MedicalRecordResponseDto
                {
                    Id = m.Id,
                    PatientId = m.PatientId,
                    PatientName = m.Patient!.FirstName + " " + m.Patient.LastName,
                    RecordDate = m.RecordDate,
                    Diagnosis = m.Diagnosis,
                    Treatment = m.Treatment,
                    Notes = m.Notes,

                    CreatedBy = m.CreatedBy,
                    CreatedAt = m.CreatedAt,
                    UpdatedBy = m.UpdatedBy,
                    UpdatedAt = m.UpdatedAt
                })
                .ToList();
        }

        public MedicalRecordResponseDto CreateMedicalRecord(
            MedicalRecordCreateDto recordDto,
            string userId)
        {
            if (!IsValidMedicalRecord(recordDto))
            {
                throw new ArgumentException("Invalid medical record details.");
            }

            var record = new MedicalRecord
            {
                PatientId = recordDto.PatientId,
                RecordDate = recordDto.RecordDate,
                Diagnosis = recordDto.Diagnosis.Trim(),
                Treatment = recordDto.Treatment?.Trim(),
                Notes = recordDto.Notes?.Trim(),

                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.MedicalRecords.Add(record);
            _context.SaveChanges();

            return GetMedicalRecordById(record.Id)!;
        }

        public bool UpdateMedicalRecord(
            int id,
            MedicalRecordCreateDto recordDto,
            string userId)
        {
            var record = _context.MedicalRecords
                .FirstOrDefault(m => m.Id == id);

            if (record == null)
                return false;

            if (!IsValidMedicalRecord(recordDto))
            {
                throw new ArgumentException("Invalid medical record details.");
            }

            record.PatientId = recordDto.PatientId;
            record.RecordDate = recordDto.RecordDate;
            record.Diagnosis = recordDto.Diagnosis.Trim();
            record.Treatment = recordDto.Treatment?.Trim();
            record.Notes = recordDto.Notes?.Trim();

            record.UpdatedBy = userId;
            record.UpdatedAt = DateTime.UtcNow;

            _context.SaveChanges();

            return true;
        }

        public bool DeleteMedicalRecord(int id)
        {
            var record = _context.MedicalRecords
                .FirstOrDefault(m => m.Id == id);

            if (record == null)
                return false;

            _context.MedicalRecords.Remove(record);
            _context.SaveChanges();

            return true;
        }

        private bool IsValidMedicalRecord(MedicalRecordCreateDto recordDto)
        {
            return recordDto.PatientId > 0
                && recordDto.RecordDate > DateTime.MinValue
                && recordDto.RecordDate <= DateTime.Now
                && !string.IsNullOrWhiteSpace(recordDto.Diagnosis)
                && recordDto.Diagnosis.Trim().Length >= 2
                && recordDto.Diagnosis.Length <= 200
                && (recordDto.Treatment == null ||
                    recordDto.Treatment.Length <= 500)
                && (recordDto.Notes == null ||
                    recordDto.Notes.Length <= 2000)
                && _context.Patients.Any(p =>
                    p.Id == recordDto.PatientId);
        }
    }
}
