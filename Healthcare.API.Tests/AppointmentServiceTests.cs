
using Healthcare.API.Data;
using Healthcare.API.DTOs;
using Healthcare.API.Enums;
using Healthcare.API.Models;
using Healthcare.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Healthcare.API.Tests
{
    public class AppointmentServiceTests
    {
        private static (
            HealthcareDbContext Context,
            AppointmentService Service
        ) CreateService()
        {
            var options = new DbContextOptionsBuilder<HealthcareDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var context = new HealthcareDbContext(options);

            context.Patients.Add(new Patient
            {
                Id = 1,
                FirstName = "Test",
                LastName = "Patient",
                Email = "patient@example.com",
                DateOfBirth = new DateOnly(1995, 5, 15)
            });

            context.Providers.Add(new Provider
            {
                Id = 1,
                FirstName = "Test",
                LastName = "Doctor",
                Specialization = "General Medicine",
                Email = "doctor@example.com",
                YearsOfExperience = 5
            });

            context.SaveChanges();

            var logger = new Mock<ILogger<AppointmentService>>();

            var service = new AppointmentService(context, logger.Object);

            return (context, service);
        }

        private static AppointmentCreateDto CreateValidDto()
        {
            return new AppointmentCreateDto
            {
                PatientId = 1,
                ProviderId = 1,
                AppointmentDate = DateTime.Now.AddDays(5),
                Reason = "General health checkup"
            };
        }

        private static Appointment AddAppointment(
            HealthcareDbContext context,
            string status = "Scheduled")
        {
            var appointment = new Appointment
            {
                PatientId = 1,
                ProviderId = 1,
                AppointmentDate = DateTime.Now.AddDays(5),
                Reason = "General health checkup",
                Status = status
            };

            context.Appointments.Add(appointment);
            context.SaveChanges();

            return appointment;
        }

        [Fact]
        public void GetAllAppointments_ReturnsAllAppointments()
        {
            var (context, service) = CreateService();

            AddAppointment(context);
            AddAppointment(context);

            var result = service.GetAllAppointments();

            Assert.Equal(2, result.Count);
            Assert.Equal("Test Patient", result[0].PatientName);
            Assert.Equal("Test Doctor", result[0].ProviderName);
        }

        [Fact]
        public void GetAppointmentById_ExistingAppointment_ReturnsAppointment()
        {
            var (context, service) = CreateService();

            var appointment = AddAppointment(context);

            var result = service.GetAppointmentById(appointment.Id);

            Assert.NotNull(result);
            Assert.Equal(appointment.Id, result.Id);
            Assert.Equal("Test Patient", result.PatientName);
            Assert.Equal("Test Doctor", result.ProviderName);
        }

        [Fact]
        public void GetAppointmentById_MissingAppointment_ReturnsNull()
        {
            var (_, service) = CreateService();

            var result = service.GetAppointmentById(999);

            Assert.Null(result);
        }

        [Fact]
        public void CreateAppointment_ValidData_CreatesAppointment()
        {
            var (context, service) = CreateService();

            var dto = CreateValidDto();

            var result = service.CreateAppointment(dto);

            Assert.True(result.Id > 0);
            Assert.Equal(1, result.PatientId);
            Assert.Equal(1, result.ProviderId);
            Assert.Equal("General health checkup", result.Reason);
            Assert.Equal(
                AppointmentStatus.Scheduled.ToString(),
                result.Status);

            Assert.Single(context.Appointments);
        }

        [Fact]
        public void CreateAppointment_InvalidPatientId_ThrowsArgumentException()
        {
            var (_, service) = CreateService();

            var dto = CreateValidDto();
            dto.PatientId = 999;

            Assert.Throws<ArgumentException>(
                () => service.CreateAppointment(dto));
        }

        [Fact]
        public void CreateAppointment_InvalidProviderId_ThrowsArgumentException()
        {
            var (_, service) = CreateService();

            var dto = CreateValidDto();
            dto.ProviderId = 999;

            Assert.Throws<ArgumentException>(
                () => service.CreateAppointment(dto));
        }

        [Fact]
        public void CreateAppointment_PastDate_ThrowsArgumentException()
        {
            var (_, service) = CreateService();

            var dto = CreateValidDto();
            dto.AppointmentDate = DateTime.Now.AddDays(-1);

            Assert.Throws<ArgumentException>(
                () => service.CreateAppointment(dto));
        }

        [Fact]
        public void CreateAppointment_InvalidReason_ThrowsArgumentException()
        {
            var (_, service) = CreateService();

            var dto = CreateValidDto();
            dto.Reason = "  ";

            Assert.Throws<ArgumentException>(
                () => service.CreateAppointment(dto));
        }

        [Fact]
        public void UpdateAppointment_ExistingAppointment_UpdatesDetails()
        {
            var (context, service) = CreateService();

            var appointment = AddAppointment(context);

            var dto = CreateValidDto();
            dto.Reason = "Updated medical consultation";
            dto.AppointmentDate = DateTime.Now.AddDays(10);

            var result = service.UpdateAppointment(appointment.Id, dto);

            Assert.True(result);
            Assert.Equal(
                "Updated medical consultation",
                context.Appointments.First().Reason);
        }

        [Fact]
        public void UpdateAppointment_MissingAppointment_ReturnsFalse()
        {
            var (_, service) = CreateService();

            var result = service.UpdateAppointment(
                999,
                CreateValidDto());

            Assert.False(result);
        }

        [Fact]
        public void UpdateAppointment_InvalidData_ThrowsArgumentException()
        {
            var (context, service) = CreateService();

            var appointment = AddAppointment(context);

            var dto = CreateValidDto();
            dto.AppointmentDate = DateTime.Now.AddDays(-1);

            Assert.Throws<ArgumentException>(
                () => service.UpdateAppointment(appointment.Id, dto));
        }

        [Theory]
        [InlineData("Scheduled", "Confirmed")]
        [InlineData("Scheduled", "Cancelled")]
        [InlineData("Confirmed", "Completed")]
        [InlineData("Confirmed", "Cancelled")]
        public void UpdateAppointmentStatus_ValidTransition_UpdatesStatus(
            string currentStatus,
            string newStatus)
        {
            var (context, service) = CreateService();

            var appointment = AddAppointment(context, currentStatus);

            var result = service.UpdateAppointmentStatus(
                appointment.Id,
                newStatus);

            Assert.True(result);
            Assert.Equal(newStatus, appointment.Status);
        }

        [Theory]
        [InlineData("Scheduled", "Completed")]
        [InlineData("Completed", "Cancelled")]
        [InlineData("Cancelled", "Confirmed")]
        [InlineData("Confirmed", "Scheduled")]
        public void UpdateAppointmentStatus_InvalidTransition_ThrowsArgumentException(
            string currentStatus,
            string newStatus)
        {
            var (context, service) = CreateService();

            var appointment = AddAppointment(context, currentStatus);

            Assert.Throws<ArgumentException>(
                () => service.UpdateAppointmentStatus(
                    appointment.Id,
                    newStatus));
        }

        [Fact]
        public void UpdateAppointmentStatus_InvalidStatus_ThrowsArgumentException()
        {
            var (_, service) = CreateService();

            Assert.Throws<ArgumentException>(
                () => service.UpdateAppointmentStatus(1, "InvalidStatus"));
        }

        [Fact]
        public void UpdateAppointmentStatus_MissingAppointment_ReturnsFalse()
        {
            var (_, service) = CreateService();

            var result = service.UpdateAppointmentStatus(999, "Confirmed");

            Assert.False(result);
        }

        [Fact]
        public void DeleteAppointment_ExistingAppointment_DeletesAppointment()
        {
            var (context, service) = CreateService();

            var appointment = AddAppointment(context);

            var result = service.DeleteAppointment(appointment.Id);

            Assert.True(result);
            Assert.Empty(context.Appointments);
        }

        [Fact]
        public void DeleteAppointment_MissingAppointment_ReturnsFalse()
        {
            var (context, service) = CreateService();

            var result = service.DeleteAppointment(999);

            Assert.False(result);
            Assert.Empty(context.Appointments);
        }
    }
}
