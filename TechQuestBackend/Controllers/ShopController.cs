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
        public async Task<IActionResult> GetItems([FromQuery] string category = "character")
        {
            if (!TryGetCategory(category, out string categoryName))
                return BadRequest("Category must be character or avatar.");

            var query = _db.ShopItems.AsNoTracking().Where(i =>
                categoryName == "character" ? i.CharacterItems != null : i.AvatarItems != null);

            var items = await query.OrderBy(i => i.ItemId).ToListAsync();
            var ownedIds = new List<int>();

            if (User.IsInRole("student"))
            {
                var profile = await GetStudentProfile();
                if (profile == null)
                    return BadRequest("Student profile was not found.");

                ownedIds = await _db.UserInventories
                    .Where(i => i.StudentId == profile.StudentId)
                    .Select(i => i.ItemId)
                    .ToListAsync();
            }

            return Ok(items.Select(i => new
            {
                i.ItemId,
                i.ItemName,
                i.AvatarItems,
                i.CharacterItems,
                i.PriceCoins,
                i.CreatedAt,
                Owned = ownedIds.Contains(i.ItemId) || i.PriceCoins == 0
            }));
        }

        [HttpGet("coins")]
        [Authorize(Roles = "student")]
        public async Task<IActionResult> GetCoins()
        {
            var profile = await GetStudentProfile();
            return profile == null
                ? BadRequest("Student profile was not found.")
                : Ok(new { totalCoins = profile.TotalCoins });
        }

        [HttpGet("owned")]
        [Authorize(Roles = "student")]
        public async Task<IActionResult> GetOwned([FromQuery] string category = "character")
        {
            if (!TryGetCategory(category, out string categoryName))
                return BadRequest("Category must be character or avatar.");

            var profile = await GetStudentProfile();
            if (profile == null)
                return BadRequest("Student profile was not found.");

            var items = await _db.ShopItems
                .AsNoTracking()
                .Where(i => (categoryName == "character" ? i.CharacterItems != null : i.AvatarItems != null)
                    && (i.PriceCoins == 0 || i.InventoryItems.Any(v => v.StudentId == profile.StudentId)))
                .OrderBy(i => i.ItemId)
                .ToListAsync();

            return Ok(items);
        }

        [HttpPost("items")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> CreateItem(ShopItemRequest request)
        {
            string validationError = ValidateItemRequest(request);
            if (!string.IsNullOrEmpty(validationError))
                return BadRequest(validationError);

            string category = request.Category.Trim().ToLowerInvariant();

            var item = new ShopItem
            {
                ItemName = request.ItemName.Trim(),
                AvatarItems = category == "avatar" ? request.ImagePath : null,
                CharacterItems = category == "character" ? request.ImagePath : null,
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

            string validationError = ValidateItemRequest(request);
            if (!string.IsNullOrEmpty(validationError))
                return BadRequest(validationError);

            string category = request.Category.Trim().ToLowerInvariant();

            item.ItemName = request.ItemName.Trim();
            item.AvatarItems = category == "avatar" ? request.ImagePath : null;
            item.CharacterItems = category == "character" ? request.ImagePath : null;
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
                return BadRequest("OWNED");

            if (profile.TotalCoins < item.PriceCoins)
                return BadRequest("Not enough Coins.");

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

        [HttpPost("equip/avatar")]
        [Authorize(Roles = "student")]
        public async Task<IActionResult> EquipAvatar(EquipItemRequest request)
        {
            var profile = await GetStudentProfile();
            if (profile == null)
                return BadRequest("Student profile was not found.");

            var item = await _db.ShopItems.AsNoTracking()
                .SingleOrDefaultAsync(i => i.ItemId == request.ItemId && i.AvatarItems != null);
            if (item == null)
                return NotFound("Avatar item was not found.");

            bool ownsItem = item.PriceCoins == 0 || await _db.UserInventories
                .AnyAsync(i => i.StudentId == profile.StudentId && i.ItemId == request.ItemId);
            if (!ownsItem)
                return BadRequest("You can only equip an avatar item you own.");

            var user = await _db.Users.FindAsync(profile.UserId);
            if (user == null)
                return Unauthorized();

            user.Avatar = item.AvatarItems;
            await _db.SaveChangesAsync();
            return Ok(new { item.ItemId, user.Avatar });
        }

        private async Task<StudentProfile?> GetStudentProfile()
        {
            if (!int.TryParse(User.FindFirstValue("userId"), out int userId))
                return null;

            return await _db.StudentProfiles.SingleOrDefaultAsync(p => p.UserId == userId);
        }

        private static bool TryGetCategory(string category, out string categoryName)
        {
            categoryName = (category ?? "").Trim().ToLowerInvariant();
            return categoryName == "character" || categoryName == "avatar";
        }

        private static string ValidateItemRequest(ShopItemRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ItemName))
                return "Item name is required.";
            if (!TryGetCategory(request.Category, out string categoryName))
                return "Category must be character or avatar.";
            if (request.PriceCoins < 0)
                return "Price cannot be negative.";
            if (string.IsNullOrWhiteSpace(request.ImagePath))
                return $"A {categoryName} image is required.";

            string extension = Path.GetExtension(request.ImagePath).ToLowerInvariant();
            if (extension != ".png" && extension != ".jpg" && extension != ".jpeg")
                return "Invalid image format. Use .png, .jpg, or .jpeg.";

            return "";
        }

        private int GetUserId()
        {
            return int.TryParse(User.FindFirstValue("userId"), out int userId) ? userId : 0;
        }
    }

    public class ShopItemRequest
    {
        public string ItemName { get; set; } = "";
        public string Category { get; set; } = "character";
        public string ImagePath { get; set; } = "";
        public int PriceCoins { get; set; }
    }

    public class PurchaseItemRequest
    {
        public int ItemId { get; set; }
    }

    public class EquipItemRequest
    {
        public int ItemId { get; set; }
    }
}
