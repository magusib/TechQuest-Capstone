namespace TechQuestBackend.Models
{
    public class RoomResult
    {
        public int RoomResultId { get; set; }

        public int RoomId { get; set; }

        public int StudentId { get; set; }

        public int Score { get; set; }

        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

        public MultiplayerRoom? Room { get; set; }

        public StudentProfile? Student { get; set; }
    }
}
