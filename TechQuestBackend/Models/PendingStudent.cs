namespace TechQuestBackend.Models
{
    public class PendingStudent
    {
        public int Id { get; set; }

        public string FirstName { get; set; } = "";

        public string LastName { get; set; } = "";

        public string Email { get; set; } = "";

        public int YearLevel { get; set; }

        public string PasswordHash { get; set; } = "";

        public DateTime Expiration { get; set; }

        public bool Verified { get; set; }
    }
}