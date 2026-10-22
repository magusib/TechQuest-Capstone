namespace TechQuestBackend.Models
{
    /// <summary>
    /// Maps to the existing PostgreSQL Student_profile table.
    ///
    /// Student_profile:
    /// - Student_id
    /// - User_id
    /// - Total_coins
    /// - Total_points
    /// </summary>
    public class StudentProfile
    {
        public int StudentId { get; set; }

        public int UserId { get; set; }

        public int TotalCoins { get; set; }

        public int TotalPoints { get; set; }

        public User? User { get; set; }

        public Character? Character { get; set; }

        public ICollection<UserInventory> InventoryItems { get; set; } = new List<UserInventory>();

        public ICollection<GameResult> GameResults { get; set; } = new List<GameResult>();

        public ICollection<NpcChallenge> NpcChallenges { get; set; } = new List<NpcChallenge>();

        public ICollection<Leaderboard> LeaderboardEntries { get; set; } = new List<Leaderboard>();

        public ICollection<StudentQuest> StudentQuests { get; set; } = new List<StudentQuest>();

        public ICollection<RoomParticipant> RoomParticipants { get; set; } = new List<RoomParticipant>();

        public ICollection<RoomResult> RoomResults { get; set; } = new List<RoomResult>();
    }
}
