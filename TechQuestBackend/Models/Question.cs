namespace TechQuestBackend.Models
{
    public class Question
    {
        public int QuestionId { get; set; }

        public int? NpcId { get; set; }

        public int? DeploymentId { get; set; }

        public int? MiniGameId { get; set; }

        public string SubjectName { get; set; } = "";

        public string QuestionText { get; set; } = "";

        public string AnswerChoices { get; set; } = "[]";

        public string CorrectAnswer { get; set; } = "";

        public string AnswerTip { get; set; } = "";

        public int TimerSeconds { get; set; } = 30;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? CreatedBy { get; set; }

        public Npc? Npc { get; set; }

        public NpcDeployment? Deployment { get; set; }

        public MiniGame? MiniGame { get; set; }

        public User? CreatedByUser { get; set; }

        public ICollection<GameResult> GameResults { get; set; } = new List<GameResult>();

        public ICollection<NpcChallenge> NpcChallenges { get; set; } = new List<NpcChallenge>();
    }
}
