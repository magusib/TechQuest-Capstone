namespace TechQuestBackend.Models
{
    public class RoomQuizAttempt
    {
        public int RoomQuizAttemptId { get; set; }

        public int RoomId { get; set; }

        public int StudentId { get; set; }

        public Guid ClientRequestId { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        public MultiplayerRoom? Room { get; set; }

        public StudentProfile? Student { get; set; }

        public ICollection<RoomResult> Results { get; set; } = new List<RoomResult>();
    }
}
