namespace TechQuestBackend.Models
{
    public class StudentQuest
    {
        public int StudentQuestId { get; set; }

        public int StudentId { get; set; }

        public int QuestId { get; set; }

        public string Status { get; set; } = "not_started";

        public DateTime? CompletedAt { get; set; }

        public bool RewardClaimed { get; set; }

        public DateTime? ClaimedAt { get; set; }

        public StudentProfile? Student { get; set; }

        public Quest? Quest { get; set; }
    }
}
