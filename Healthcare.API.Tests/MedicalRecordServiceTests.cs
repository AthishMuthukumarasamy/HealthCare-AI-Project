
using Healthcare.API.Data;
using Healthcare.API.DTOs;
using Healthcare.API.Models;
using Healthcare.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Healthcare.API.Tests
{
    public class MedicalRecordServiceTests
    {
        private static (
            HealthcareDbContext Context,
            MedicalRecordService Service
        ) CreateService()
        {
            var options = new DbContextOptionsBuilder<HealthcareDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(warnings =>
                    warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            var context = new HealthcareDbContext(options);

            context.Patients.AddRange(
                new Patient
                {
                    Id = 1,
                    FirstName = "Test",
                    LastName = "Patient One",
                    Email = "patient1@example.com",
                    DateOfBirth = new DateOnly(1995, 5, 15)
                },
                new Patient
                {
                    Id = 2,
                    FirstName = "Test",
                    LastName = "Patient Two",
                    Email = "patient2@example.com",
                    DateOfBirth = new DateOnly(1990, 8, 20)
                });

            context.SaveChanges();

            var logger = new Mock<ILogger<MedicalRecordService>>();

            var service = new MedicalRecordService(context, logger.Object);

            return (context, service);
        }

        private static MedicalRecordCreateDto CreateValidDto()
        {
            return new MedicalRecordCreateDto
            {
                PatientId = 1,
                RecordDate = DateTime.Now.AddDays(-1),
                Diagnosis = "Common cold",
                Treatment = "Rest and fluids",
                Notes = "Patient advised to rest."
            };
        }

        private static MedicalRecord AddMedicalRecord(
            HealthcareDbContext context,
            int patientId = 1)
        {
            var record = new MedicalRecord
            {
                PatientId = patientId,
                RecordDate = DateTime.Now.AddDays(-1),
                Diagnosis = "Common cold",
                Treatment = "Rest and fluids",
                Notes = "Initial medical record.",
                CreatedBy = "test-admin",
                CreatedAt = DateTime.UtcNow
            };

            context.MedicalRecords.Add(record);
            context.SaveChanges();

            return record;
        }

        [Fact]
        public void GetAllMedicalRecords_ReturnsAllRecords()
        {
            var (context, service) = CreateService();

            AddMedicalRecord(context);
            AddMedicalRecord(context, 2);

            var result = service.GetAllMedicalRecords();

            Assert.Equal(2, result.Count);
            Assert.Equal("Test Patient One", result[0].PatientName);
            Assert.Equal("Test Patient Two", result[1].PatientName);
        }

        [Fact]
        public void GetMedicalRecordById_ExistingRecord_ReturnsRecord()
        {
            var (context, service) = CreateService();

            var record = AddMedicalRecord(context);

            var result = service.GetMedicalRecordById(record.Id);

            Assert.NotNull(result);
            Assert.Equal(record.Id, result.Id);
            Assert.Equal("Common cold", result.Diagnosis);
            Assert.Equal("Test Patient One", result.PatientName);
        }

        [Fact]
        public void GetMedicalRecordById_MissingRecord_ReturnsNull()
        {
            var (_, service) = CreateService();

            var result = service.GetMedicalRecordById(999);

            Assert.Null(result);
        }

        [Fact]
        public void GetMedicalRecordsByPatientId_ReturnsOnlyMatchingRecords()
        {
            var (context, service) = CreateService();

            AddMedicalRecord(context, 1);
            AddMedicalRecord(context, 1);
            AddMedicalRecord(context, 2);

            var result = service.GetMedicalRecordsByPatientId(1);

            Assert.Equal(2, result.Count);
            Assert.All(result, item => Assert.Equal(1, item.PatientId));
        }

        [Fact]
        public void GetMedicalRecordAuditHistory_ReturnsEntriesInChronologicalOrder()
        {
            var (context, service) = CreateService();

            var record = AddMedicalRecord(context);
            var firstTime = DateTime.UtcNow.AddMinutes(-10);
            var secondTime = DateTime.UtcNow;

            context.MedicalRecordAuditLogs.AddRange(
                new MedicalRecordAuditLog
                {
                    MedicalRecordId = record.Id,
                    OriginalMedicalRecordId = record.Id,
                    Action = "UPDATE",
                    PerformedBy = "doctor-1",
                    PerformedAt = secondTime,
                    Changes = "Diagnosis updated."
                },
                new MedicalRecordAuditLog
                {
                    MedicalRecordId = record.Id,
                    OriginalMedicalRecordId = record.Id,
                    Action = "CREATE",
                    PerformedBy = "admin-1",
                    PerformedAt = firstTime,
                    Changes = "Medical record created."
                });

            context.SaveChanges();

            var result = service.GetMedicalRecordAuditHistory(record.Id);

            Assert.Equal(2, result.Count);
            Assert.Equal("CREATE", result[0].Action);
            Assert.Equal("UPDATE", result[1].Action);
        }

        [Fact]
        public void CreateMedicalRecord_ValidData_CreatesRecordAndAuditLog()
        {
            var (context, service) = CreateService();

            var result = service.CreateMedicalRecord(
                CreateValidDto(),
                "test-admin");

            Assert.True(result.Id > 0);
            Assert.Equal("Common cold", result.Diagnosis);
            Assert.Equal("Test Patient One", result.PatientName);
            Assert.Equal("test-admin", result.CreatedBy);
            Assert.NotEqual(default, result.CreatedAt);

            var audit = Assert.Single(context.MedicalRecordAuditLogs);
            Assert.Equal("CREATE", audit.Action);
            Assert.Equal("test-admin", audit.PerformedBy);
            Assert.Equal(result.Id, audit.OriginalMedicalRecordId);
        }

        [Fact]
        public void CreateMedicalRecord_InvalidPatientId_ThrowsArgumentException()
        {
            var (_, service) = CreateService();

            var dto = CreateValidDto();
            dto.PatientId = 999;

            Assert.Throws<ArgumentException>(
                () => service.CreateMedicalRecord(dto, "test-admin"));
        }

        [Fact]
        public void CreateMedicalRecord_FutureRecordDate_ThrowsArgumentException()
        {
            var (_, service) = CreateService();

            var dto = CreateValidDto();
            dto.RecordDate = DateTime.Now.AddDays(1);

            Assert.Throws<ArgumentException>(
                () => service.CreateMedicalRecord(dto, "test-admin"));
        }

        [Fact]
        public void CreateMedicalRecord_InvalidDiagnosis_ThrowsArgumentException()
        {
            var (_, service) = CreateService();

            var dto = CreateValidDto();
            dto.Diagnosis = " ";

            Assert.Throws<ArgumentException>(
                () => service.CreateMedicalRecord(dto, "test-admin"));
        }

        [Fact]
        public void UpdateMedicalRecord_ExistingRecord_UpdatesDetailsAndCreatesAuditLog()
        {
            var (context, service) = CreateService();

            var record = AddMedicalRecord(context);
            var dto = CreateValidDto();

            dto.Diagnosis = "Updated diagnosis";
            dto.Treatment = "Updated treatment";
            dto.Notes = "Updated notes";

            var result = service.UpdateMedicalRecord(
                record.Id,
                dto,
                "test-doctor");

            Assert.True(result);
            Assert.Equal("Updated diagnosis", record.Diagnosis);
            Assert.Equal("test-doctor", record.UpdatedBy);
            Assert.NotNull(record.UpdatedAt);

            var audit = Assert.Single(context.MedicalRecordAuditLogs);
            Assert.Equal("UPDATE", audit.Action);
            Assert.Equal("test-doctor", audit.PerformedBy);
            Assert.Contains("Diagnosis updated.", audit.Changes);
        }

        [Fact]
        public void UpdateMedicalRecord_NoChanges_ReturnsTrueWithoutAuditLog()
        {
            var (context, service) = CreateService();

            var record = AddMedicalRecord(context);

            var dto = new MedicalRecordCreateDto
            {
                PatientId = record.PatientId,
                RecordDate = record.RecordDate,
                Diagnosis = record.Diagnosis,
                Treatment = record.Treatment,
                Notes = record.Notes
            };

            var result = service.UpdateMedicalRecord(
                record.Id,
                dto,
                "test-doctor");

            Assert.True(result);
            Assert.Empty(context.MedicalRecordAuditLogs);
            Assert.Null(record.UpdatedBy);
        }

        [Fact]
        public void UpdateMedicalRecord_MissingRecord_ReturnsFalse()
        {
            var (_, service) = CreateService();

            var result = service.UpdateMedicalRecord(
                999,
                CreateValidDto(),
                "test-doctor");

            Assert.False(result);
        }

        [Fact]
        public void UpdateMedicalRecord_InvalidData_ThrowsArgumentException()
        {
            var (context, service) = CreateService();

            var record = AddMedicalRecord(context);
            var dto = CreateValidDto();
            dto.RecordDate = DateTime.Now.AddDays(1);

            Assert.Throws<ArgumentException>(
                () => service.UpdateMedicalRecord(
                    record.Id,
                    dto,
                    "test-doctor"));
        }

        [Fact]
        public void DeleteMedicalRecord_ExistingRecord_DeletesRecordAndPreservesAuditLog()
        {
            var (context, service) = CreateService();

            var record = AddMedicalRecord(context);
            var originalId = record.Id;

            var result = service.DeleteMedicalRecord(
                originalId,
                "test-admin");

            Assert.True(result);
            Assert.Empty(context.MedicalRecords);

            var audit = Assert.Single(context.MedicalRecordAuditLogs);
            Assert.Equal("DELETE", audit.Action);
            Assert.Equal("test-admin", audit.PerformedBy);
            Assert.Equal(originalId, audit.OriginalMedicalRecordId);
            Assert.Null(audit.MedicalRecordId);
        }

        [Fact]
        public void DeleteMedicalRecord_MissingRecord_ReturnsFalse()
        {
            var (context, service) = CreateService();

            var result = service.DeleteMedicalRecord(999, "test-admin");

            Assert.False(result);
            Assert.Empty(context.MedicalRecords);
            Assert.Empty(context.MedicalRecordAuditLogs);
        }
    }
}
