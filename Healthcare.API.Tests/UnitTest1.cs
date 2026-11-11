
using Healthcare.API.Data;
using Healthcare.API.DTOs;
using Healthcare.API.Models;
using Healthcare.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Healthcare.API.Tests
{
    public class PatientServiceTests
    {
        private static (HealthcareDbContext Context, PatientService Service) CreateService()
        {
            var options = new DbContextOptionsBuilder<HealthcareDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var context = new HealthcareDbContext(options);
            var logger = new Mock<ILogger<PatientService>>();

            var service = new PatientService(context, logger.Object);

            return (context, service);
        }

        private static PatientCreateDto CreatePatientDto()
        {
            return new PatientCreateDto
            {
                FirstName = "Arun",
                LastName = "Kumar",
                Email = "arun.test@example.com",
                DateOfBirth = new DateOnly(1998, 5, 15),
                PhoneNumber = "9876543210"
            };
        }

        [Fact]
        public void GetAllPatients_ReturnsAllPatients()
        {
            using var context = CreateService().Context;
            var service = new PatientService(
                context,
                Mock.Of<ILogger<PatientService>>());

            context.Patients.AddRange(
                new Patient
                {
                    FirstName = "Arun",
                    LastName = "Kumar",
                    Email = "arun@example.com",
                    DateOfBirth = new DateOnly(1998, 5, 15)
                },
                new Patient
                {
                    FirstName = "Priya",
                    LastName = "Sharma",
                    Email = "priya@example.com",
                    DateOfBirth = new DateOnly(1995, 8, 20)
                });

            context.SaveChanges();

            var result = service.GetAllPatients();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, p => p.FirstName == "Arun");
            Assert.Contains(result, p => p.FirstName == "Priya");
        }

        [Fact]
        public void GetPatientById_ExistingPatient_ReturnsPatient()
        {
            using var context = CreateService().Context;
            var service = new PatientService(
                context,
                Mock.Of<ILogger<PatientService>>());

            var patient = new Patient
            {
                FirstName = "Arun",
                LastName = "Kumar",
                Email = "arun@example.com",
                DateOfBirth = new DateOnly(1998, 5, 15)
            };

            context.Patients.Add(patient);
            context.SaveChanges();

            var result = service.GetPatientById(patient.Id);

            Assert.NotNull(result);
            Assert.Equal(patient.Id, result.Id);
            Assert.Equal("Arun", result.FirstName);
        }

        [Fact]
        public void GetPatientById_MissingPatient_ReturnsNull()
        {
            var (_, service) = CreateService();

            var result = service.GetPatientById(999);

            Assert.Null(result);
        }

        [Fact]
        public void CreatePatient_ValidData_CreatesPatient()
        {
            using var context = CreateService().Context;
            var service = new PatientService(
                context,
                Mock.Of<ILogger<PatientService>>());

            var result = service.CreatePatient(CreatePatientDto());

            Assert.True(result.Id > 0);
            Assert.Equal("Arun", result.FirstName);
            Assert.Equal("Kumar", result.LastName);
            Assert.Equal("arun.test@example.com", result.Email);
            Assert.Single(context.Patients);
        }

        [Fact]
        public void UpdatePatient_ExistingPatient_UpdatesDetailsButPreservesEmail()
        {
            using var context = CreateService().Context;
            var service = new PatientService(
                context,
                Mock.Of<ILogger<PatientService>>());

            var patient = new Patient
            {
                FirstName = "Arun",
                LastName = "Kumar",
                Email = "arun@example.com",
                DateOfBirth = new DateOnly(1998, 5, 15),
                PhoneNumber = "9876543210"
            };

            context.Patients.Add(patient);
            context.SaveChanges();

            var updateDto = new PatientUpdateDto
            {
                FirstName = "Arun Updated",
                LastName = "Kumar",
                DateOfBirth = new DateOnly(1998, 5, 15),
                PhoneNumber = "9999999999"
            };

            var result = service.UpdatePatient(patient.Id, updateDto);

            Assert.True(result);

            var updatedPatient = context.Patients.Find(patient.Id);

            Assert.NotNull(updatedPatient);
            Assert.Equal("Arun Updated", updatedPatient.FirstName);
            Assert.Equal("arun@example.com", updatedPatient.Email);
            Assert.Equal("9999999999", updatedPatient.PhoneNumber);
        }

        [Fact]
        public void UpdatePatient_MissingPatient_ReturnsFalse()
        {
            var (_, service) = CreateService();

            var updateDto = new PatientUpdateDto
            {
                FirstName = "Arun",
                LastName = "Kumar",
                DateOfBirth = new DateOnly(1998, 5, 15),
                PhoneNumber = "9876543210"
            };

            var result = service.UpdatePatient(999, updateDto);

            Assert.False(result);
        }

        [Fact]
        public void DeletePatient_ExistingPatient_DeletesPatient()
        {
            using var context = CreateService().Context;
            var service = new PatientService(
                context,
                Mock.Of<ILogger<PatientService>>());

            var patient = new Patient
            {
                FirstName = "Arun",
                LastName = "Kumar",
                Email = "arun.delete@example.com",
                DateOfBirth = new DateOnly(1998, 5, 15)
            };

            context.Patients.Add(patient);
            context.SaveChanges();

            var result = service.DeletePatient(patient.Id);

            Assert.True(result);
            Assert.Empty(context.Patients);
        }

        [Fact]
        public void DeletePatient_MissingPatient_ReturnsFalse()
        {
            var (_, service) = CreateService();

            var result = service.DeletePatient(999);

            Assert.False(result);
        }
    }
}
