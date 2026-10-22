namespace TechQuestBackend.Models
{
    public class QuestProgressEvent
    {
        public int QuestProgressEventId { get; set; }

        public int StudentQuestId { get; set; }

        public string EventId { get; set; } = "";

        public string EventType { get; set; } = "";

        public int Amount { get; set; }

        public int? NpcId { get; set; }

        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

        public StudentQuest? StudentQuest { get; set; }
    }
}
