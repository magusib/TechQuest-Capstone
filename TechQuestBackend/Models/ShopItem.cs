namespace TechQuestBackend.Models
{
    public class ShopItem
    {
        public int ItemId { get; set; }

        public string ItemName { get; set; } = "";

        public string? AvatarItems { get; set; }

        public string? CharacterItems { get; set; }

        public int PriceCoins { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<UserInventory> InventoryItems { get; set; } = new List<UserInventory>();
    }
}
