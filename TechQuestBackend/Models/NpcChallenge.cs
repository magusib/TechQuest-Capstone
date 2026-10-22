namespace TechQuestBackend.Models
{
    public class NpcChallenge
    {
        public int ChallengeId { get; set; }

        public int StudentId { get; set; }

        public int DeploymentId { get; set; }

        public int QuestionId { get; set; }

        public int NpcId { get; set; }

        public string NpcName { get; set; } = "";

        public string Difficulty { get; set; } = "";

        public string SubjectName { get; set; } = "";

        public string MiniGameType { get; set; } = "";

        public string QuestionText { get; set; } = "";

        public string AnswerChoices { get; set; } = "[]";

        public string CorrectAnswer { get; set; } = "";

        public string AnswerTip { get; set; } = "";

        public int TimerSeconds { get; set; }

        public Guid AttemptId { get; set; }

        public string State { get; set; } = "MiniGamePending";

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public DateTime? QuestionStartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public bool? MiniGameWon { get; set; }

        public bool AnswerTipAwarded { get; set; }

        public string? SelectedAnswer { get; set; }

        public bool? QuestionCorrect { get; set; }

        public bool QuestionTimedOut { get; set; }

        public int CoinsChanged { get; set; }

        public int PointsChanged { get; set; }

        public StudentProfile? Student { get; set; }

        public NpcDeployment? Deployment { get; set; }

        public Question? Question { get; set; }
    }
}
