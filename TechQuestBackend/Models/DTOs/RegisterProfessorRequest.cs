namespace TechQuestBackend.Models.DTOs
{
    public class RegisterProfessorRequest
    {
        public string FirstName { get; set; } = "";

        public string LastName { get; set; } = "";

        public string Email { get; set; } = "";

        public string Password { get; set; } = "";

        public string ConfirmPassword { get; set; } = "";
    }
}
