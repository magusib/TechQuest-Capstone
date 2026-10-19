namespace TechQuestBackend.Models
{
    public class GameResult
    {
        public int ResultId { get; set; }

        public int StudentId { get; set; }

        public int QuestionId { get; set; }

        public int EarnedPoints { get; set; }

        public int EarnedCoins { get; set; }

        public int CoinsDeducted { get; set; }

        public bool IsCorrect { get; set; }

        public Guid? AttemptId { get; set; }

        public DateTime PlayedAt { get; set; } = DateTime.UtcNow;

        public StudentProfile? Student { get; set; }

        public Question? Question { get; set; }
    }
}
