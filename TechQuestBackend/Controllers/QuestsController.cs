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
            var now = DateTime.UtcNow;
            var studentProfile = await GetStudentProfileAsync();

            var questQuery = _db.Quests.AsNoTracking();
            if (User.IsInRole("admin"))
            {
                int userId = GetUserId();
                questQuery = questQuery.Where(q => q.CreatedBy == userId);
            }
            else
            {
                questQuery = questQuery.Where(q =>
                    q.StartDate <= now && (!q.EndDate.HasValue || q.EndDate.Value >= now));
            }

            var quests = User.IsInRole("admin")
                ? await questQuery
                    .OrderByDescending(q => q.CreatedAt)
                    .ThenByDescending(q => q.QuestId)
                    .ToListAsync()
                : await questQuery
                    .OrderBy(q => q.StartDate)
                    .ToListAsync();

            var result = new List<QuestStatusDto>();

            foreach (var quest in quests)
            {
                var studentQuest = studentProfile == null
                    ? null
                    : await _db.StudentQuests
                        .AsNoTracking()
                        .SingleOrDefaultAsync(sq => sq.StudentId == studentProfile.StudentId && sq.QuestId == quest.QuestId);

                var isCompleted = studentQuest != null && string.Equals(studentQuest.Status, "completed", StringComparison.OrdinalIgnoreCase);
                var statusText = isCompleted ? "Completed" : "In Progress";

                result.Add(new QuestStatusDto
                {
                    QuestId = quest.QuestId,
                    QuestTitle = quest.QuestTitle,
                    Description = quest.Description,
                    Difficulty = quest.Difficulty,
                    RewardPoints = quest.RewardPoints,
                    RewardCoins = quest.RewardCoins,
                    StartDate = quest.StartDate,
                    EndDate = quest.EndDate,
                    Status = statusText,
                    RewardClaimed = studentQuest != null && studentQuest.RewardClaimed,
                    ClaimedAt = studentQuest?.ClaimedAt,
                    CompletedAt = studentQuest?.CompletedAt,
                    IsCompleted = isCompleted
                });
            }

            return Ok(result);
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

            var difficulty = string.IsNullOrWhiteSpace(request.Difficulty) ? "Easy" : request.Difficulty.Trim();
            if (!string.Equals(difficulty, "Easy", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(difficulty, "Medium", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(difficulty, "Hard", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Quest difficulty must be Easy, Medium, or Hard.");
            }

            if (string.Equals(difficulty, "Easy", StringComparison.OrdinalIgnoreCase))
                difficulty = "Easy";
            else if (string.Equals(difficulty, "Medium", StringComparison.OrdinalIgnoreCase))
                difficulty = "Medium";
            else
                difficulty = "Hard";

            var quest = new Quest
            {
                QuestTitle = request.QuestTitle.Trim(),
                Description = request.Description,
                Difficulty = difficulty,
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
            var profile = await _db.StudentProfiles.SingleOrDefaultAsync(p => p.UserId == userId);
            if (profile == null)
                return BadRequest("Student profile was not found.");

            var quest = await _db.Quests.FindAsync(questId);
            if (quest == null)
                return NotFound("Quest was not found.");

            var studentQuest = await _db.StudentQuests
                .SingleOrDefaultAsync(q => q.StudentId == profile.StudentId && q.QuestId == questId);

            if (studentQuest == null)
            {
                studentQuest = new StudentQuest
                {
                    StudentId = profile.StudentId,
                    QuestId = questId,
                    Status = "completed",
                    CompletedAt = DateTime.UtcNow,
                    RewardClaimed = false,
                    ClaimedAt = null
                };
                _db.StudentQuests.Add(studentQuest);
                await _db.SaveChangesAsync();
                return Ok(new { studentQuest.StudentQuestId, questId, status = studentQuest.Status, rewardClaimed = studentQuest.RewardClaimed });
            }

            if (string.Equals(studentQuest.Status, "completed", StringComparison.OrdinalIgnoreCase))
            {
                return Ok(new { studentQuest.StudentQuestId, questId, status = studentQuest.Status, rewardClaimed = studentQuest.RewardClaimed });
            }

            studentQuest.Status = "completed";
            studentQuest.CompletedAt = DateTime.UtcNow;
            studentQuest.RewardClaimed = false;
            studentQuest.ClaimedAt = null;

            await _db.SaveChangesAsync();
            return Ok(new { studentQuest.StudentQuestId, questId, status = studentQuest.Status, rewardClaimed = studentQuest.RewardClaimed });
        }

        [HttpPost("{questId:int}/claim-reward")]
        [Authorize(Roles = "student")]
        public async Task<IActionResult> ClaimReward(int questId)
        {
            int userId = GetUserId();
            var profile = await _db.StudentProfiles.SingleOrDefaultAsync(p => p.UserId == userId);
            if (profile == null)
                return BadRequest("Student profile was not found.");

            var quest = await _db.Quests.FindAsync(questId);
            if (quest == null)
                return NotFound("Quest was not found.");

            var studentQuest = await _db.StudentQuests
                .SingleOrDefaultAsync(q => q.StudentId == profile.StudentId && q.QuestId == questId);

            if (studentQuest == null)
                return BadRequest("Quest was not found for this student.");

            if (!string.Equals(studentQuest.Status, "completed", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Quest is not complete yet.");

            if (studentQuest.RewardClaimed)
                return BadRequest("Quest reward was already claimed.");

            profile.TotalPoints += quest.RewardPoints;
            profile.TotalCoins += quest.RewardCoins;

            studentQuest.RewardClaimed = true;
            studentQuest.ClaimedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(new
            {
                studentQuest.StudentQuestId,
                rewards = new { points = quest.RewardPoints, coins = quest.RewardCoins },
                totalCoins = profile.TotalCoins,
                totalPoints = profile.TotalPoints,
                rewardClaimed = studentQuest.RewardClaimed,
                claimedAt = studentQuest.ClaimedAt
            });
        }

        private async Task<StudentProfile?> GetStudentProfileAsync()
        {
            int userId = GetUserId();
            if (userId <= 0)
            {
                return null;
            }

            return await _db.StudentProfiles.SingleOrDefaultAsync(p => p.UserId == userId);
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
        public string Difficulty { get; set; } = "Easy";
        public int RewardPoints { get; set; }
        public int RewardCoins { get; set; }
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime? EndDate { get; set; }
    }

    public class QuestStatusDto
    {
        public int QuestId { get; set; }
        public string QuestTitle { get; set; } = "";
        public string? Description { get; set; }
        public string Difficulty { get; set; } = "Easy";
        public int RewardPoints { get; set; }
        public int RewardCoins { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; } = "In Progress";
        public bool RewardClaimed { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? ClaimedAt { get; set; }
        public bool IsCompleted { get; set; }
    }
}
