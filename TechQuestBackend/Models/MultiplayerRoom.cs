namespace TechQuestBackend.Models
{
    public class MultiplayerRoom
    {
        public int RoomId { get; set; }

        public string RoomCode { get; set; } = "";

        public string RoomName { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string Status { get; set; } = "waiting";

        public int? CreatedBy { get; set; }

        public User? CreatedByUser { get; set; }

        public ICollection<RoomParticipant> Participants { get; set; } = new List<RoomParticipant>();

        public ICollection<RoomResult> Results { get; set; } = new List<RoomResult>();
    }
}
