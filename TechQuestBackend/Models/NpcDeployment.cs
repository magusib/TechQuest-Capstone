namespace TechQuestBackend.Models
{
    public class NpcDeployment
    {
        public int DeploymentId { get; set; }

        public int NpcId { get; set; }

        public Npc? Npc { get; set; }

        public ICollection<Question> Questions { get; set; } = new List<Question>();

        public string MapDifficulty { get; set; } = "Easy";

        public float? PositionX { get; set; }

        public float? PositionY { get; set; }

        public string? PositionMarker { get; set; }

        public string Status { get; set; } = "Pending";

        public DateTime DeployedAt { get; set; } = DateTime.UtcNow;

        public int? DeployedBy { get; set; }

        public User? DeployedByUser { get; set; }
    }
}
