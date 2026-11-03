namespace TechQuestBackend.Models
{
    public class CharacterChallengeSession
    {
        public int CharacterChallengeSessionId { get; set; }
        public int StudentId { get; set; }
        public int DeploymentId { get; set; }
        public int NpcId { get; set; }
        public string NpcName { get; set; } = "";
        public string CharacterName { get; set; } = "";
        public Guid AttemptId { get; set; }
        public int PlayerScore { get; set; }
        public int NpcScore { get; set; }
        public string? Outcome { get; set; }
        public int CoinsDeducted { get; set; }
        public int PointsDeducted { get; set; }
        public string State { get; set; } = "Pending";
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        public StudentProfile? Student { get; set; }
        public NpcDeployment? Deployment { get; set; }
        public Npc? Npc { get; set; }
    }
}
