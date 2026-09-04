namespace TechQuestBackend.Models
{
    public class RoomParticipant
    {
        public int ParticipantsId { get; set; }

        public int RoomId { get; set; }

        public int UserId { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public MultiplayerRoom? Room { get; set; }

        public StudentProfile? Student { get; set; }
    }
}
