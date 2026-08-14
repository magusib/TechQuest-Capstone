namespace TechQuestBackend.Models
{
    public class PasswordReset
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Email { get; set; } = "";

        public string OtpCode { get; set; } = "";

        public string NewPasswordHash { get; set; } = "";

        public DateTime ExpiresAt { get; set; }

        public bool IsUsed { get; set; }

        public int Attempts { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
