using System;

namespace TechQuestBackend.Models
{
    public class OTP
    {
        public int Id { get; set; }

        public string Email { get; set; } = "";

        public string Code { get; set; } = "";

        public DateTime Expiration { get; set; }

        public bool Used { get; set; }

        public int Attempts { get; set; }

        public string PendingPasswordHash { get; set; } = "";
    }
}