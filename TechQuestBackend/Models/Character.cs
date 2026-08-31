namespace TechQuestBackend.Models
{
    public class Character
    {
        public int CharacterId { get; set; }

        public int StudentId { get; set; }

        public int ItemId { get; set; }

        public StudentProfile? Student { get; set; }

        public ShopItem? Item { get; set; }
    }
}
