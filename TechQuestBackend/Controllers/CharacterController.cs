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
    [Authorize(Roles = "student")]
    public class CharacterController : ControllerBase
    {
        private readonly TechQuestDbContext _db;

        public CharacterController(TechQuestDbContext db)
        {
            _db = db;
        }

        [HttpGet("selected")]
        public async Task<IActionResult> Selected()
        {
            int userId = GetUserId();
            var profile = await _db.StudentProfiles.AsNoTracking().SingleOrDefaultAsync(p => p.UserId == userId);
            if (profile == null)
                return BadRequest("Student profile was not found.");

            var selected = await _db.Characters
                .AsNoTracking()
                .Include(c => c.Item)
                .SingleOrDefaultAsync(c => c.StudentId == profile.StudentId);

            if (selected == null)
                return Ok(null);

            return Ok(new
            {
                selected.ItemId,
                Item = selected.Item == null ? null : new
                {
                    selected.Item.ItemId,
                    selected.Item.ItemName,
                    selected.Item.CharacterItems
                }
            });
        }

        [HttpPost("select")]
        public async Task<IActionResult> Select(SelectCharacterRequest request)
        {
            int userId = GetUserId();
            var profile = await _db.StudentProfiles.SingleOrDefaultAsync(p => p.UserId == userId);
            if (profile == null)
                return BadRequest("Student profile was not found.");

            bool ownsItem = await _db.UserInventories
                .AnyAsync(i => i.StudentId == profile.StudentId && i.ItemId == request.ItemId);

            if (!ownsItem)
                return BadRequest("You can only select a character item you own.");

            var item = await _db.ShopItems
                .AsNoTracking()
                .SingleOrDefaultAsync(i => i.ItemId == request.ItemId
                    && !string.IsNullOrWhiteSpace(i.CharacterItems)
                    && string.IsNullOrWhiteSpace(i.AvatarItems));

            if (item == null)
                return BadRequest("The selected item is not a valid character item.");

            var selected = await _db.Characters.SingleOrDefaultAsync(c => c.StudentId == profile.StudentId);
            if (selected == null)
            {
                selected = new Character { StudentId = profile.StudentId, ItemId = request.ItemId };
                _db.Characters.Add(selected);
            }
            else
            {
                selected.ItemId = request.ItemId;
            }

            await _db.SaveChangesAsync();
            return Ok(new
            {
                selected.ItemId,
                Item = new
                {
                    item.ItemId,
                    item.ItemName,
                    item.CharacterItems
                }
            });
        }

        private int GetUserId()
        {
            return int.TryParse(User.FindFirstValue("userId"), out int userId) ? userId : 0;
        }
    }

    public class SelectCharacterRequest
    {
        public int ItemId { get; set; }
    }
}
