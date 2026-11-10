
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

        // GET: All Medical Records
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

        // GET: Medical Record By ID
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

        // GET: Medical Records By Patient ID
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

        // GET: Medical Record Audit History
        public List<MedicalRecordAuditLogResponseDto> GetMedicalRecordAuditHistory(
            int medicalRecordId)
        {
            return _context.MedicalRecordAuditLogs
                .Where(a => a.OriginalMedicalRecordId == medicalRecordId)
                .OrderBy(a => a.PerformedAt)
                .Select(a => new MedicalRecordAuditLogResponseDto
                {
                    Id = a.Id,
                    MedicalRecordId = a.MedicalRecordId,
                    OriginalMedicalRecordId = a.OriginalMedicalRecordId,
                    Action = a.Action,
                    PerformedBy = a.PerformedBy,
                    PerformedAt = a.PerformedAt,
                    Changes = a.Changes
                })
                .ToList();
        }

        // CREATE: Medical Record with Audit Logging
        public MedicalRecordResponseDto CreateMedicalRecord(
            MedicalRecordCreateDto recordDto,
            string userId)
        {
            if (!IsValidMedicalRecord(recordDto))
            {
                throw new ArgumentException("Invalid medical record details.");
            }

            using var transaction = _context.Database.BeginTransaction();

            try
            {
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

                var auditLog = new MedicalRecordAuditLog
                {
                    MedicalRecordId = record.Id,
                    OriginalMedicalRecordId = record.Id,
                    Action = "CREATE",
                    PerformedBy = userId,
                    PerformedAt = DateTime.UtcNow,
                    Changes = "Medical record created."
                };

                _context.MedicalRecordAuditLogs.Add(auditLog);
                _context.SaveChanges();

                transaction.Commit();

                return GetMedicalRecordById(record.Id)!;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // UPDATE: Medical Record with Audit Logging
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

            var changes = new List<string>();

            if (record.PatientId != recordDto.PatientId)
                changes.Add("PatientId updated.");

            if (record.RecordDate != recordDto.RecordDate)
                changes.Add("RecordDate updated.");

            if (record.Diagnosis != recordDto.Diagnosis.Trim())
                changes.Add("Diagnosis updated.");

            if (record.Treatment != recordDto.Treatment?.Trim())
                changes.Add("Treatment updated.");

            if (record.Notes != recordDto.Notes?.Trim())
                changes.Add("Notes updated.");

            if (changes.Count == 0)
                return true;

            using var transaction = _context.Database.BeginTransaction();

            try
            {
                record.PatientId = recordDto.PatientId;
                record.RecordDate = recordDto.RecordDate;
                record.Diagnosis = recordDto.Diagnosis.Trim();
                record.Treatment = recordDto.Treatment?.Trim();
                record.Notes = recordDto.Notes?.Trim();

                record.UpdatedBy = userId;
                record.UpdatedAt = DateTime.UtcNow;

                var auditLog = new MedicalRecordAuditLog
                {
                    MedicalRecordId = record.Id,
                    OriginalMedicalRecordId = record.Id,
                    Action = "UPDATE",
                    PerformedBy = userId,
                    PerformedAt = DateTime.UtcNow,
                    Changes = string.Join("; ", changes)
                };

                _context.MedicalRecordAuditLogs.Add(auditLog);
                _context.SaveChanges();

                transaction.Commit();

                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // DELETE: Medical Record with Audit Logging
        public bool DeleteMedicalRecord(int id, string userId)
        {
            var record = _context.MedicalRecords
                .FirstOrDefault(m => m.Id == id);

            if (record == null)
                return false;

            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var auditLog = new MedicalRecordAuditLog
                {
                    MedicalRecordId = record.Id,
                    OriginalMedicalRecordId = record.Id,
                    Action = "DELETE",
                    PerformedBy = userId,
                    PerformedAt = DateTime.UtcNow,
                    Changes = "Medical record deleted."
                };

                _context.MedicalRecordAuditLogs.Add(auditLog);
                _context.SaveChanges();

                _context.MedicalRecords.Remove(record);
                _context.SaveChanges();

                transaction.Commit();

                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // Medical Record Validation
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
