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
    public class NpcsController : ControllerBase
    {
        private readonly TechQuestDbContext _db;

        public NpcsController(TechQuestDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        [Authorize(Roles = "student,admin")]
        public async Task<IActionResult> Get()
        {
            return Ok(await _db.Npcs.AsNoTracking().OrderBy(n => n.NpcId).ToListAsync());
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create(NpcRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NpcName))
                return BadRequest("NPC name is required.");

            var npc = new Npc
            {
                NpcName = request.NpcName.Trim(),
                NpcImage = request.NpcImage,
                Status = string.IsNullOrWhiteSpace(request.Status) ? "active" : request.Status.Trim(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = GetUserId()
            };

            _db.Npcs.Add(npc);
            await _db.SaveChangesAsync();
            return Ok(npc);
        }

        [HttpPut("{npcId:int}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Update(int npcId, NpcRequest request)
        {
            var npc = await _db.Npcs.FindAsync(npcId);
            if (npc == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(request.NpcName))
                return BadRequest("NPC name is required.");

            npc.NpcName = request.NpcName.Trim();
            npc.NpcImage = request.NpcImage;
            npc.Status = string.IsNullOrWhiteSpace(request.Status) ? npc.Status : request.Status.Trim();
            await _db.SaveChangesAsync();
            return Ok(npc);
        }

        [HttpDelete("{npcId:int}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int npcId)
        {
            var npc = await _db.Npcs.FindAsync(npcId);
            if (npc == null)
                return NotFound();

            _db.Npcs.Remove(npc);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        private int GetUserId()
        {
            return int.TryParse(User.FindFirstValue("userId"), out int userId) ? userId : 0;
        }
    }

    public class NpcRequest
    {
        public string NpcName { get; set; } = "";
        public string? NpcImage { get; set; }
        public string? Status { get; set; }
    }
}
