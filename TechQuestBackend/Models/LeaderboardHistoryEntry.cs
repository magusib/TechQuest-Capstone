namespace TechQuestBackend.Models
{
    public class LeaderboardHistoryEntry
    {
        public int LeaderboardHistoryEntryId { get; set; }

        public int LeaderboardHistoryId { get; set; }

        public int StudentId { get; set; }

        public string FirstName { get; set; } = "";

        public string LastName { get; set; } = "";

        public int? YearLevel { get; set; }

        public string? Avatar { get; set; }

        public int TotalPoints { get; set; }

        public int RankPosition { get; set; }

        public LeaderboardHistory? History { get; set; }
    }
}