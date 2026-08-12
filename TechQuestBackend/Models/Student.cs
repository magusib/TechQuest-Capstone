namespace TechQuestBackend.Models
{
    public class Student
    {
        public int Id { get; set; }

        public string FirstName { get; set; } = "";

        public string LastName { get; set; } = "";

        public string Email { get; set; } = "";

        public string PasswordHash { get; set; } = "";

        public int YearLevel { get; set; }

        public bool EmailVerified { get; set; }
    }
}