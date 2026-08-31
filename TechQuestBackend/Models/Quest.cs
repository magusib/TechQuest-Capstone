namespace TechQuestBackend.Models
{
    public class Quest
    {
        public int QuestId { get; set; }

        public string QuestTitle { get; set; } = "";

        public string? Description { get; set; }

        public int RewardPoints { get; set; }

        public int RewardCoins { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? CreatedBy { get; set; }

        public User? CreatedByUser { get; set; }

        public ICollection<StudentQuest> StudentQuests { get; set; } = new List<StudentQuest>();
    }
}
