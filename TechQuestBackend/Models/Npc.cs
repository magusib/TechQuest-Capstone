namespace TechQuestBackend.Models
{
    public class Npc
    {
        public int NpcId { get; set; }

        public string NpcName { get; set; } = "";

        public string? NpcImage { get; set; }

        public string Status { get; set; } = "active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? CreatedBy { get; set; }

        public User? CreatedByUser { get; set; }

        public ICollection<Question> Questions { get; set; } = new List<Question>();
    }
}
