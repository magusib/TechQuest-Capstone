namespace TechQuestBackend.Models.DTOs
{
    public class RegisterStudentRequest
    {
        public string FirstName { get; set; } = "";

        public string LastName { get; set; } = "";

        public string Email { get; set; } = "";

        public int YearLevel { get; set; }

        public string Password { get; set; } = "";

        public string ConfirmPassword { get; set; } = "";
    }
}