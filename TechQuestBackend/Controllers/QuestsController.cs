using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechQuestBackend.Data;
using TechQuestBackend.Models;
using TechQuestBackend.Services;
using System.Data;

namespace TechQuestBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class QuestsController : ControllerBase
    {
        private static readonly HashSet<string> SupportedMiniGameTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "FallingDuck",
            "MatchThePairs",
            "QuickTapChallenge"
        };

        private readonly TechQuestDbContext _db;
        private readonly IQuestProgressService _questProgressService;

        public QuestsController(TechQuestDbContext db, IQuestProgressService questProgressService)
        {
            _db = db;
            _questProgressService = questProgressService;
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
                questQuery = questQuery.Where(q =>
                    q.CreatedBy == userId &&
                    (!q.EndDate.HasValue || q.EndDate.Value >= now));
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
                    ObjectiveType = quest.ObjectiveType,
                    TargetQuantity = quest.TargetQuantity,
                    RequiredMiniGameType = quest.RequiredMiniGameType,
                    RequiredMiniGameId = quest.RequiredMiniGameId,
                    RequireMiniGameSuccess = quest.RequireMiniGameSuccess,
                    Progress = studentQuest?.Progress ?? 0,
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

            string objectiveType = string.IsNullOrWhiteSpace(request.ObjectiveType)
                ? "manual"
                : request.ObjectiveType.Trim().ToLowerInvariant();
            if (objectiveType is not ("manual" or "npc_challenge" or "mini_game" or "coins" or "points"))
                return BadRequest("Quest objective type is invalid.");

            if (request.TargetQuantity <= 0)
                return BadRequest("Quest target quantity must be greater than zero.");

            if (request.EndDate.HasValue && request.EndDate.Value < request.StartDate)
                return BadRequest("End date cannot be before start date.");

            if (objectiveType == "mini_game")
            {
                request.RequiredMiniGameType = request.RequiredMiniGameType?.Trim();
                var requiredGame = request.RequiredMiniGameId.HasValue
                    ? await _db.MiniGames.SingleOrDefaultAsync(game => game.MiniGameId == request.RequiredMiniGameId.Value)
                    : null;
                if (request.RequiredMiniGameId.HasValue && requiredGame == null)
                    return BadRequest("The required mini-game was not found.");
                if (string.IsNullOrWhiteSpace(request.RequiredMiniGameType) && requiredGame != null)
                    request.RequiredMiniGameType = requiredGame.GameType;
                string? supportedGameType = SupportedMiniGameTypes.FirstOrDefault(gameType =>
                    string.Equals(gameType, request.RequiredMiniGameType, StringComparison.OrdinalIgnoreCase));
                if (supportedGameType == null ||
                    (requiredGame != null && !string.Equals(
                        requiredGame.GameType,
                        supportedGameType,
                        StringComparison.OrdinalIgnoreCase)) ||
                    !await _db.MiniGames.AnyAsync(game => game.GameType == supportedGameType))
                    return BadRequest("Select a valid mini-game type.");
                request.RequiredMiniGameType = supportedGameType;
            }
            else if (!string.IsNullOrWhiteSpace(request.RequiredMiniGameType) ||
                request.RequiredMiniGameId.HasValue ||
                request.RequireMiniGameSuccess)
            {
                return BadRequest("Mini-game requirements can only be configured for mini-game objectives.");
            }

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
                ObjectiveType = objectiveType,
                TargetQuantity = request.TargetQuantity,
                RequiredMiniGameType = request.RequiredMiniGameType,
                RequiredMiniGameId = request.RequiredMiniGameId,
                RequireMiniGameSuccess = request.RequireMiniGameSuccess,
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

            if (!string.Equals(quest.ObjectiveType, "manual", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Automatic quest progress is recorded from completed game events.");

            var studentQuest = await _db.StudentQuests
                .SingleOrDefaultAsync(q => q.StudentId == profile.StudentId && q.QuestId == questId);

            if (studentQuest == null)
            {
                studentQuest = new StudentQuest
                {
                    StudentId = profile.StudentId,
                    QuestId = questId,
                    Status = "completed",
                    Progress = quest.TargetQuantity,
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
            studentQuest.Progress = quest.TargetQuantity;
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
            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            var profile = await _db.StudentProfiles
                .FromSqlInterpolated($"SELECT * FROM \"Student_profile\" WHERE \"UserId\" = {userId} FOR UPDATE")
                .SingleOrDefaultAsync();
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

            string rewardEventId = $"quest-reward:{studentQuest.StudentQuestId}";
            await _questProgressService.ApplyEventAsync(
                profile.StudentId,
                rewardEventId,
                "coins",
                quest.RewardCoins);
            await _questProgressService.ApplyEventAsync(
                profile.StudentId,
                rewardEventId,
                "points",
                quest.RewardPoints);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
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
        public string ObjectiveType { get; set; } = "manual";
        public int TargetQuantity { get; set; } = 1;
        public string? RequiredMiniGameType { get; set; }
        public int? RequiredMiniGameId { get; set; }
        public bool RequireMiniGameSuccess { get; set; }
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
        public string ObjectiveType { get; set; } = "manual";
        public int TargetQuantity { get; set; } = 1;
        public string? RequiredMiniGameType { get; set; }
        public int? RequiredMiniGameId { get; set; }
        public bool RequireMiniGameSuccess { get; set; }
        public int Progress { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; } = "In Progress";
        public bool RewardClaimed { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? ClaimedAt { get; set; }
        public bool IsCompleted { get; set; }
    }
}
