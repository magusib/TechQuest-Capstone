namespace TechQuestBackend.Models
{
    public class Professor
    {
        public int Id { get; set; }

        public string Email { get; set; } = "";

        public string PasswordHash { get; set; } = "";

        public bool EmailVerified { get; set; }
    }
}