
using Healthcare.API.Data;
using Healthcare.API.DTOs;
using Healthcare.API.Enums;
using Healthcare.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Healthcare.API.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly HealthcareDbContext _context;
        private readonly ILogger<AppointmentService> _logger;

        public AppointmentService(
            HealthcareDbContext context,
            ILogger<AppointmentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<AppointmentResponseDto> GetAllAppointments()
        {
            _logger.LogInformation(
                "Retrieving all appointments.");

            var appointments = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Provider)
                .Select(a => new AppointmentResponseDto
                {
                    Id = a.Id,
                    PatientId = a.PatientId,
                    PatientName = a.Patient!.FirstName + " " + a.Patient.LastName,
                    ProviderId = a.ProviderId,
                    ProviderName = a.Provider!.FirstName + " " + a.Provider.LastName,
                    AppointmentDate = a.AppointmentDate,
                    Reason = a.Reason,
                    Status = a.Status
                })
                .ToList();

            _logger.LogInformation(
                "Successfully retrieved {AppointmentCount} appointments.",
                appointments.Count);

            return appointments;
        }

        public AppointmentResponseDto? GetAppointmentById(int id)
        {
            _logger.LogInformation(
                "Retrieving appointment with ID {AppointmentId}.", id);

            var appointment = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Provider)
                .FirstOrDefault(a => a.Id == id);

            if (appointment == null)
            {
                _logger.LogWarning(
                    "Appointment with ID {AppointmentId} was not found.",
                    id);

                return null;
            }

            _logger.LogInformation(
                "Successfully retrieved appointment with ID {AppointmentId}.",
                id);

            return new AppointmentResponseDto
            {
                Id = appointment.Id,
                PatientId = appointment.PatientId,
                PatientName = appointment.Patient!.FirstName + " " + appointment.Patient.LastName,
                ProviderId = appointment.ProviderId,
                ProviderName = appointment.Provider!.FirstName + " " + appointment.Provider.LastName,
                AppointmentDate = appointment.AppointmentDate,
                Reason = appointment.Reason,
                Status = appointment.Status
            };
        }

        public AppointmentResponseDto CreateAppointment(
            AppointmentCreateDto appointmentDto)
        {
            _logger.LogInformation(
                "Creating a new appointment.");

            if (!IsValidAppointment(appointmentDto))
            {
                _logger.LogWarning(
                    "Appointment creation failed because the supplied details are invalid.");

                throw new ArgumentException("Invalid appointment details.");
            }

            var appointment = new Appointment
            {
                PatientId = appointmentDto.PatientId,
                ProviderId = appointmentDto.ProviderId,
                AppointmentDate = appointmentDto.AppointmentDate,
                Reason = appointmentDto.Reason.Trim(),
                Status = AppointmentStatus.Scheduled.ToString()
            };

            _context.Appointments.Add(appointment);
            _context.SaveChanges();

            _logger.LogInformation(
                "Appointment created successfully with ID {AppointmentId}.",
                appointment.Id);

            return GetAppointmentById(appointment.Id)!;
        }

        public bool UpdateAppointment(
            int id,
            AppointmentCreateDto appointmentDto)
        {
            _logger.LogInformation(
                "Updating appointment with ID {AppointmentId}.", id);

            var appointment = _context.Appointments
                .FirstOrDefault(a => a.Id == id);

            if (appointment == null)
            {
                _logger.LogWarning(
                    "Cannot update appointment. Appointment with ID {AppointmentId} was not found.",
                    id);

                return false;
            }

            if (!IsValidAppointment(appointmentDto))
            {
                _logger.LogWarning(
                    "Appointment update failed because the supplied details are invalid for appointment ID {AppointmentId}.",
                    id);

                throw new ArgumentException("Invalid appointment details.");
            }

            appointment.PatientId = appointmentDto.PatientId;
            appointment.ProviderId = appointmentDto.ProviderId;
            appointment.AppointmentDate = appointmentDto.AppointmentDate;
            appointment.Reason = appointmentDto.Reason.Trim();

            _context.SaveChanges();

            _logger.LogInformation(
                "Appointment with ID {AppointmentId} updated successfully.",
                id);

            return true;
        }

        public bool UpdateAppointmentStatus(int id, string status)
        {
            _logger.LogInformation(
                "Updating status for appointment ID {AppointmentId}.",
                id);

            if (!Enum.TryParse<AppointmentStatus>(
                    status, true, out var parsedStatus)
                || !Enum.IsDefined(typeof(AppointmentStatus), parsedStatus))
            {
                _logger.LogWarning(
                    "Invalid status supplied for appointment ID {AppointmentId}.",
                    id);

                throw new ArgumentException("Invalid appointment status.");
            }

            var appointment = _context.Appointments
                .FirstOrDefault(a => a.Id == id);

            if (appointment == null)
            {
                _logger.LogWarning(
                    "Cannot update status. Appointment with ID {AppointmentId} was not found.",
                    id);

                return false;
            }

            bool isValidTransition = appointment.Status switch
            {
                nameof(AppointmentStatus.Scheduled) =>
                    parsedStatus == AppointmentStatus.Confirmed ||
                    parsedStatus == AppointmentStatus.Cancelled,

                nameof(AppointmentStatus.Confirmed) =>
                    parsedStatus == AppointmentStatus.Completed ||
                    parsedStatus == AppointmentStatus.Cancelled,

                _ => false
            };

            if (!isValidTransition)
            {
                _logger.LogWarning(
                    "Invalid status transition attempted for appointment ID {AppointmentId}.",
                    id);

                throw new ArgumentException(
                    $"Cannot change appointment status from {appointment.Status} to {parsedStatus}.");
            }

            appointment.Status = parsedStatus.ToString();

            _context.SaveChanges();

            _logger.LogInformation(
                "Status for appointment ID {AppointmentId} updated successfully.",
                id);

            return true;
        }

        public bool DeleteAppointment(int id)
        {
            _logger.LogInformation(
                "Deleting appointment with ID {AppointmentId}.", id);

            var appointment = _context.Appointments
                .FirstOrDefault(a => a.Id == id);

            if (appointment == null)
            {
                _logger.LogWarning(
                    "Cannot delete appointment. Appointment with ID {AppointmentId} was not found.",
                    id);

                return false;
            }

            _context.Appointments.Remove(appointment);
            _context.SaveChanges();

            _logger.LogInformation(
                "Appointment with ID {AppointmentId} deleted successfully.",
                id);

            return true;
        }

        private bool IsValidAppointment(
            AppointmentCreateDto appointmentDto)
        {
            return appointmentDto.PatientId > 0
                && appointmentDto.ProviderId > 0
                && appointmentDto.AppointmentDate != default
                && appointmentDto.AppointmentDate > DateTime.Now
                && !string.IsNullOrWhiteSpace(appointmentDto.Reason)
                && appointmentDto.Reason.Trim().Length >= 3
                && appointmentDto.Reason.Trim().Length <= 500
                && _context.Patients.Any(p => p.Id == appointmentDto.PatientId)
                && _context.Providers.Any(p => p.Id == appointmentDto.ProviderId);
        }
    }
}
