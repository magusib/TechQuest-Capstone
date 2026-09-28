namespace TechQuestBackend.Models
{
    public class LeaderboardHistory
    {
        public int LeaderboardHistoryId { get; set; }

        public DateTime ResetAt { get; set; }

        public ICollection<LeaderboardHistoryEntry> Entries { get; set; } = new List<LeaderboardHistoryEntry>();
    }
}