
using Healthcare.API.Data;
using Healthcare.API.DTOs;
using Healthcare.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Healthcare.API.Services
{
    public class MedicalRecordService : IMedicalRecordService
    {
        private readonly HealthcareDbContext _context;
        private readonly ILogger<MedicalRecordService> _logger;

        public MedicalRecordService(
            HealthcareDbContext context,
            ILogger<MedicalRecordService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: All Medical Records
        public List<MedicalRecordResponseDto> GetAllMedicalRecords()
        {
            _logger.LogInformation("Retrieving all medical records.");

            var records = _context.MedicalRecords
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

            _logger.LogInformation(
                "Retrieved {RecordCount} medical records.",
                records.Count);

            return records;
        }

        // GET: Medical Record By ID
        public MedicalRecordResponseDto? GetMedicalRecordById(int id)
        {
            _logger.LogInformation(
                "Retrieving medical record with ID {MedicalRecordId}.",
                id);

            var record = _context.MedicalRecords
                .Include(m => m.Patient)
                .FirstOrDefault(m => m.Id == id);

            if (record == null)
            {
                _logger.LogWarning(
                    "Medical record with ID {MedicalRecordId} was not found.",
                    id);

                return null;
            }

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
            _logger.LogInformation(
                "Retrieving medical records for patient ID {PatientId}.",
                patientId);

            var records = _context.MedicalRecords
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

            _logger.LogInformation(
                "Retrieved {RecordCount} medical records for patient ID {PatientId}.",
                records.Count,
                patientId);

            return records;
        }

        // GET: Medical Record Audit History
        public List<MedicalRecordAuditLogResponseDto> GetMedicalRecordAuditHistory(
            int medicalRecordId)
        {
            _logger.LogInformation(
                "Retrieving audit history for medical record ID {MedicalRecordId}.",
                medicalRecordId);

            var auditHistory = _context.MedicalRecordAuditLogs
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

            _logger.LogInformation(
                "Retrieved {AuditCount} audit entries for medical record ID {MedicalRecordId}.",
                auditHistory.Count,
                medicalRecordId);

            return auditHistory;
        }

        // CREATE: Medical Record with Audit Logging
        public MedicalRecordResponseDto CreateMedicalRecord(
            MedicalRecordCreateDto recordDto,
            string userId)
        {
            _logger.LogInformation("Creating a new medical record.");

            if (!IsValidMedicalRecord(recordDto))
            {
                _logger.LogWarning(
                    "Medical record creation failed validation.");

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

                _logger.LogInformation(
                    "Medical record with ID {MedicalRecordId} was created successfully.",
                    record.Id);

                return GetMedicalRecordById(record.Id)!;
            }
            catch (Exception ex)
            {
                transaction.Rollback();

                _logger.LogError(
                    ex,
                    "An error occurred while creating a medical record.");

                throw;
            }
        }

        // UPDATE: Medical Record with Audit Logging
        public bool UpdateMedicalRecord(
            int id,
            MedicalRecordCreateDto recordDto,
            string userId)
        {
            _logger.LogInformation(
                "Updating medical record with ID {MedicalRecordId}.",
                id);

            var record = _context.MedicalRecords
                .FirstOrDefault(m => m.Id == id);

            if (record == null)
            {
                _logger.LogWarning(
                    "Medical record with ID {MedicalRecordId} was not found for update.",
                    id);

                return false;
            }

            if (!IsValidMedicalRecord(recordDto))
            {
                _logger.LogWarning(
                    "Medical record with ID {MedicalRecordId} failed validation during update.",
                    id);

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
            {
                _logger.LogInformation(
                    "No changes detected for medical record ID {MedicalRecordId}.",
                    id);

                return true;
            }

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

                _logger.LogInformation(
                    "Medical record with ID {MedicalRecordId} was updated successfully.",
                    id);

                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();

                _logger.LogError(
                    ex,
                    "An error occurred while updating medical record ID {MedicalRecordId}.",
                    id);

                throw;
            }
        }

        // DELETE: Medical Record with Audit Logging
        public bool DeleteMedicalRecord(int id, string userId)
        {
            _logger.LogInformation(
                "Deleting medical record with ID {MedicalRecordId}.",
                id);

            var record = _context.MedicalRecords
                .FirstOrDefault(m => m.Id == id);

            if (record == null)
            {
                _logger.LogWarning(
                    "Medical record with ID {MedicalRecordId} was not found for deletion.",
                    id);

                return false;
            }

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

                _logger.LogInformation(
                    "Medical record with ID {MedicalRecordId} was deleted successfully.",
                    id);

                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();

                _logger.LogError(
                    ex,
                    "An error occurred while deleting medical record ID {MedicalRecordId}.",
                    id);

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
