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
    public class GameResultsController : ControllerBase
    {
        private readonly TechQuestDbContext _db;

        public GameResultsController(TechQuestDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        public async Task<IActionResult> Submit(GameResultRequest request)
        {
            if (request.EarnedCoins < 0 || request.EarnedPoints < 0)
                return BadRequest("Rewards cannot be negative.");

            int userId = GetUserId();
            await using var transaction = await _db.Database.BeginTransactionAsync();

            var profile = await _db.StudentProfiles.SingleOrDefaultAsync(p => p.UserId == userId);
            if (profile == null)
                return BadRequest("Student profile was not found.");

            bool questionExists = await _db.Questions.AnyAsync(q => q.QuestionId == request.QuestionId);
            if (!questionExists)
                return NotFound("Question was not found.");

            var result = new GameResult
            {
                StudentId = profile.StudentId,
                QuestionId = request.QuestionId,
                EarnedCoins = request.EarnedCoins,
                EarnedPoints = request.EarnedPoints,
                IsCorrect = request.IsCorrect,
                PlayedAt = DateTime.UtcNow
            };

            profile.TotalCoins += request.EarnedCoins;
            profile.TotalPoints += request.EarnedPoints;

            _db.GameResults.Add(result);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { result.ResultId, profile.TotalCoins, profile.TotalPoints });
        }

        private int GetUserId()
        {
            return int.TryParse(User.FindFirstValue("userId"), out int userId) ? userId : 0;
        }
    }

    public class GameResultRequest
    {
        public int QuestionId { get; set; }
        public int EarnedPoints { get; set; }
        public int EarnedCoins { get; set; }
        public bool IsCorrect { get; set; }
    }
}
