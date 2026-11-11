namespace Healthcare.API.DTOs
{
    public class PatientResponseDto
    {
        public int Id { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public DateOnly DateOfBirth { get; set; }
    }
}