
using Healthcare.API.DTOs;

namespace Healthcare.API.Services
{
    public interface IAppointmentService
    {
        List<AppointmentResponseDto> GetAllAppointments();

        AppointmentResponseDto? GetAppointmentById(int id);

        AppointmentResponseDto CreateAppointment(AppointmentCreateDto appointmentDto);

        bool UpdateAppointment(int id, AppointmentCreateDto appointmentDto);

        bool DeleteAppointment(int id);
    }
}
