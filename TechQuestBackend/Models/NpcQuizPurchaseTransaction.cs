namespace TechQuestBackend.Models
{
    public class NpcQuizPurchaseTransaction
    {
        public long TransactionId { get; set; }

        public int StudentId { get; set; }

        public int ChallengeId { get; set; }

        public Guid PurchaseId { get; set; }

        public string PurchaseType { get; set; } = "";

        public int CostCoins { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public StudentProfile? Student { get; set; }

        public NpcChallenge? Challenge { get; set; }
    }
}
