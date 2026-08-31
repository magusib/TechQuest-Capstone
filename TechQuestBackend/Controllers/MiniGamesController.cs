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
    public class MiniGamesController : ControllerBase
    {
        private readonly TechQuestDbContext _db;

        public MiniGamesController(TechQuestDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        [Authorize(Roles = "student,admin")]
        public async Task<IActionResult> Get()
        {
            return Ok(await _db.MiniGames.AsNoTracking().OrderBy(m => m.MiniGameId).ToListAsync());
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create(MiniGameRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.GameName) || string.IsNullOrWhiteSpace(request.GameType))
                return BadRequest("Game name and type are required.");

            var miniGame = new MiniGame
            {
                GameName = request.GameName.Trim(),
                GameType = request.GameType.Trim(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = GetUserId()
            };

            _db.MiniGames.Add(miniGame);
            await _db.SaveChangesAsync();
            return Ok(miniGame);
        }

        [HttpPut("{miniGameId:int}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Update(int miniGameId, MiniGameRequest request)
        {
            var miniGame = await _db.MiniGames.FindAsync(miniGameId);
            if (miniGame == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(request.GameName) || string.IsNullOrWhiteSpace(request.GameType))
                return BadRequest("Game name and type are required.");

            miniGame.GameName = request.GameName.Trim();
            miniGame.GameType = request.GameType.Trim();
            await _db.SaveChangesAsync();
            return Ok(miniGame);
        }

        [HttpDelete("{miniGameId:int}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int miniGameId)
        {
            var miniGame = await _db.MiniGames.FindAsync(miniGameId);
            if (miniGame == null)
                return NotFound();

            _db.MiniGames.Remove(miniGame);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        private int GetUserId()
        {
            return int.TryParse(User.FindFirstValue("userId"), out int userId) ? userId : 0;
        }
    }

    public class MiniGameRequest
    {
        public string GameName { get; set; } = "";
        public string GameType { get; set; } = "";
    }
}
