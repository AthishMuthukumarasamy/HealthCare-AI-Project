
using Healthcare.API.Data;
using Healthcare.API.DTOs;
using Healthcare.API.Enums;
using Healthcare.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Healthcare.API.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly HealthcareDbContext _context;

        public AppointmentService(HealthcareDbContext context)
        {
            _context = context;
        }

        public List<AppointmentResponseDto> GetAllAppointments()
        {
            return _context.Appointments
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
        }

        public AppointmentResponseDto? GetAppointmentById(int id)
        {
            var appointment = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Provider)
                .FirstOrDefault(a => a.Id == id);

            if (appointment == null)
                return null;

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

        public AppointmentResponseDto CreateAppointment(AppointmentCreateDto appointmentDto)
        {
            if (!IsValidAppointment(appointmentDto))
            {
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

            return GetAppointmentById(appointment.Id)!;
        }

        public bool UpdateAppointment(int id, AppointmentCreateDto appointmentDto)
        {
            var appointment = _context.Appointments
                .FirstOrDefault(a => a.Id == id);

            if (appointment == null)
                return false;

            if (!IsValidAppointment(appointmentDto))
            {
                throw new ArgumentException("Invalid appointment details.");
            }

            appointment.PatientId = appointmentDto.PatientId;
            appointment.ProviderId = appointmentDto.ProviderId;
            appointment.AppointmentDate = appointmentDto.AppointmentDate;
            appointment.Reason = appointmentDto.Reason.Trim();

            _context.SaveChanges();

            return true;
        }

        public bool UpdateAppointmentStatus(int id, string status)
        {
            if (!Enum.TryParse<AppointmentStatus>(
                    status, true, out var parsedStatus)
                || !Enum.IsDefined(typeof(AppointmentStatus), parsedStatus))
            {
                throw new ArgumentException("Invalid appointment status.");
            }

            var appointment = _context.Appointments
                .FirstOrDefault(a => a.Id == id);

            if (appointment == null)
                return false;

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
                throw new ArgumentException(
                    $"Cannot change appointment status from {appointment.Status} to {parsedStatus}.");
            }

            appointment.Status = parsedStatus.ToString();

            _context.SaveChanges();

            return true;
        }

        public bool DeleteAppointment(int id)
        {
            var appointment = _context.Appointments
                .FirstOrDefault(a => a.Id == id);

            if (appointment == null)
                return false;

            _context.Appointments.Remove(appointment);
            _context.SaveChanges();

            return true;
        }

        private bool IsValidAppointment(AppointmentCreateDto appointmentDto)
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
