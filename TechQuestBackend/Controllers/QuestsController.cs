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
    public class QuestsController : ControllerBase
    {
        private readonly TechQuestDbContext _db;

        public QuestsController(TechQuestDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        [Authorize(Roles = "student,admin")]
        public async Task<IActionResult> GetQuests()
        {
            return Ok(await _db.Quests.AsNoTracking().OrderBy(q => q.QuestId).ToListAsync());
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create(QuestRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.QuestTitle))
                return BadRequest("Quest title is required.");

            if (request.RewardCoins < 0 || request.RewardPoints < 0)
                return BadRequest("Rewards cannot be negative.");

            if (request.EndDate.HasValue && request.EndDate.Value < request.StartDate)
                return BadRequest("End date cannot be before start date.");

            var quest = new Quest
            {
                QuestTitle = request.QuestTitle.Trim(),
                Description = request.Description,
                RewardCoins = request.RewardCoins,
                RewardPoints = request.RewardPoints,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = GetUserId()
            };

            _db.Quests.Add(quest);
            await _db.SaveChangesAsync();
            return Ok(quest);
        }

        [HttpPost("{questId:int}/complete")]
        [Authorize(Roles = "student")]
        public async Task<IActionResult> Complete(int questId)
        {
            int userId = GetUserId();
            await using var transaction = await _db.Database.BeginTransactionAsync();

            var profile = await _db.StudentProfiles.SingleOrDefaultAsync(p => p.UserId == userId);
            if (profile == null)
                return BadRequest("Student profile was not found.");

            var quest = await _db.Quests.FindAsync(questId);
            if (quest == null)
                return NotFound("Quest was not found.");

            var studentQuest = await _db.StudentQuests
                .SingleOrDefaultAsync(q => q.StudentId == profile.StudentId && q.QuestId == questId);

            if (studentQuest?.Status == "completed")
                return BadRequest("Quest reward was already claimed.");

            if (studentQuest == null)
            {
                studentQuest = new StudentQuest
                {
                    StudentId = profile.StudentId,
                    QuestId = questId
                };
                _db.StudentQuests.Add(studentQuest);
            }

            studentQuest.Status = "completed";
            studentQuest.CompletedAt = DateTime.UtcNow;
            profile.TotalPoints += quest.RewardPoints;
            profile.TotalCoins += quest.RewardCoins;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Ok(new { studentQuest.StudentQuestId, profile.TotalCoins, profile.TotalPoints });
        }

        private int GetUserId()
        {
            return int.TryParse(User.FindFirstValue("userId"), out int userId) ? userId : 0;
        }
    }

    public class QuestRequest
    {
        public string QuestTitle { get; set; } = "";
        public string? Description { get; set; }
        public int RewardPoints { get; set; }
        public int RewardCoins { get; set; }
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime? EndDate { get; set; }
    }
}
