namespace TechQuestBackend.Models
{
    public class UserInventory
    {
        public int InventoryId { get; set; }

        public int StudentId { get; set; }

        public int ItemId { get; set; }

        public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;

        public StudentProfile? Student { get; set; }

        public ShopItem? Item { get; set; }
    }
}
