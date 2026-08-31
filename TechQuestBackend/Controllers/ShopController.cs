using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechQuestBackend.Data;
using TechQuestBackend.Models;

namespace TechQuestBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ShopController : ControllerBase
    {
        private readonly TechQuestDbContext _db;

        public ShopController(TechQuestDbContext db)
        {
            _db = db;
        }

        [HttpGet("items")]
        [Authorize(Roles = "student,professor,admin")]
        public async Task<IActionResult> GetItems()
        {
            return Ok(await _db.ShopItems.AsNoTracking().OrderBy(i => i.ItemId).ToListAsync());
        }

        [HttpPost("items")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> CreateItem(ShopItemRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ItemName))
                return BadRequest("Item name is required.");

            if (request.PriceCoins < 0)
                return BadRequest("Price cannot be negative.");

            var item = new ShopItem
            {
                ItemName = request.ItemName.Trim(),
                AvatarItems = request.AvatarItems,
                CharacterItems = request.CharacterItems,
                PriceCoins = request.PriceCoins,
                CreatedAt = DateTime.UtcNow
            };

            _db.ShopItems.Add(item);
            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpPut("items/{itemId:int}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateItem(int itemId, ShopItemRequest request)
        {
            var item = await _db.ShopItems.FindAsync(itemId);
            if (item == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(request.ItemName))
                return BadRequest("Item name is required.");

            if (request.PriceCoins < 0)
                return BadRequest("Price cannot be negative.");

            item.ItemName = request.ItemName.Trim();
            item.AvatarItems = request.AvatarItems;
            item.CharacterItems = request.CharacterItems;
            item.PriceCoins = request.PriceCoins;

            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpDelete("items/{itemId:int}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteItem(int itemId)
        {
            var item = await _db.ShopItems.FindAsync(itemId);
            if (item == null)
                return NotFound();

            _db.ShopItems.Remove(item);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("purchase")]
        [Authorize(Roles = "student")]
        public async Task<IActionResult> Purchase(PurchaseItemRequest request)
        {
            int userId = GetUserId();
            if (userId <= 0)
                return Unauthorized();

            await using var transaction = await _db.Database.BeginTransactionAsync();

            var profile = await _db.StudentProfiles.SingleOrDefaultAsync(p => p.UserId == userId);
            if (profile == null)
                return BadRequest("Student profile was not found.");

            var item = await _db.ShopItems.FindAsync(request.ItemId);
            if (item == null)
                return NotFound("Shop item was not found.");

            bool alreadyOwned = await _db.UserInventories
                .AnyAsync(i => i.StudentId == profile.StudentId && i.ItemId == item.ItemId);

            if (alreadyOwned)
                return BadRequest("This item is already owned.");

            if (profile.TotalCoins < item.PriceCoins)
                return BadRequest("Not enough coins.");

            profile.TotalCoins -= item.PriceCoins;
            _db.UserInventories.Add(new UserInventory
            {
                StudentId = profile.StudentId,
                ItemId = item.ItemId,
                PurchasedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { message = "Purchase successful.", profile.TotalCoins, profile.TotalPoints });
        }

        private int GetUserId()
        {
            return int.TryParse(User.FindFirstValue("userId"), out int userId) ? userId : 0;
        }
    }

    public class ShopItemRequest
    {
        public string ItemName { get; set; } = "";
        public string? AvatarItems { get; set; }
        public string? CharacterItems { get; set; }
        public int PriceCoins { get; set; }
    }

    public class PurchaseItemRequest
    {
        public int ItemId { get; set; }
    }
}
