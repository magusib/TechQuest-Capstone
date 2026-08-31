namespace TechQuestBackend.Models
{
    public class MiniGame
    {
        public int MiniGameId { get; set; }

        public string GameName { get; set; } = "";

        public string GameType { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? CreatedBy { get; set; }

        public User? CreatedByUser { get; set; }

        public ICollection<Question> Questions { get; set; } = new List<Question>();
    }
}
