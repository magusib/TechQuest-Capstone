namespace TechQuestBackend.Models
{
    public class Leaderboard
    {
        public int LeaderboardId { get; set; }

        public int StudentId { get; set; }

        public int TotalPoints { get; set; }

        public int? RankPosition { get; set; }

        public StudentProfile? Student { get; set; }
    }
}
