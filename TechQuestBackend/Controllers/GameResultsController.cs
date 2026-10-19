using System.Data;
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

            if (request.EarnedCoins > 0 || request.EarnedPoints > 0)
                return BadRequest("Rewards must be issued by a server-validated game endpoint.");

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

        [HttpPost("mini-game-attempt")]
        public async Task<IActionResult> SubmitMiniGameAttempt(MiniGameAttemptRequest request)
        {
            if (request.AttemptId == Guid.Empty)
                return BadRequest("A valid attempt identifier is required.");

            if (!IsSupportedGameType(request.GameType))
                return BadRequest("The mini-game type is invalid.");

            int userId = GetUserId();
            if (userId <= 0)
                return Unauthorized();

            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

            var profile = await _db.StudentProfiles
                .FromSqlInterpolated($"SELECT * FROM \"Student_profile\" WHERE \"UserId\" = {userId} FOR UPDATE")
                .SingleOrDefaultAsync();
            if (profile == null)
                return BadRequest("Student profile was not found.");

            bool questionBelongsToDeployment = await _db.Questions.AnyAsync(
                question => question.QuestionId == request.QuestionId &&
                    question.DeploymentId == request.DeploymentId);
            if (!questionBelongsToDeployment)
                return BadRequest("This question is not assigned to the specified NPC deployment.");

            var existingAttempt = await _db.GameResults
                .AsNoTracking()
                .FirstOrDefaultAsync(result => result.AttemptId == request.AttemptId);
            if (existingAttempt != null)
            {
                if (existingAttempt.StudentId != profile.StudentId ||
                    existingAttempt.QuestionId != request.QuestionId ||
                    existingAttempt.IsCorrect != request.GameWon)
                    return Conflict("This attempt identifier was already used for a different result.");

                await transaction.CommitAsync();
                return Ok(new MiniGameAttemptResponse
                {
                    AttemptId = request.AttemptId,
                    GameWon = existingAttempt.IsCorrect,
                    CoinsDeducted = existingAttempt.CoinsDeducted,
                    TotalCoins = profile.TotalCoins,
                    AnswerTip = existingAttempt.IsCorrect
                        ? await _db.Questions
                            .Where(question => question.QuestionId == existingAttempt.QuestionId)
                            .Select(question => question.AnswerTip)
                            .FirstOrDefaultAsync() ?? ""
                        : "",
                    AlreadyProcessed = true
                });
            }

            var question = await _db.Questions
                .Include(item => item.MiniGame)
                .FirstOrDefaultAsync(item => item.QuestionId == request.QuestionId);
            if (question == null)
                return NotFound("NPC question was not found.");

            if (question.MiniGame == null ||
                !string.Equals(question.MiniGame.GameType, request.GameType, StringComparison.Ordinal))
                return BadRequest("This mini-game is not assigned to the NPC question.");

            int coinsDeducted = 0;
            if (!request.GameWon)
            {
                coinsDeducted = Math.Min(5, profile.TotalCoins);
                profile.TotalCoins -= coinsDeducted;
            }

            var result = new GameResult
            {
                StudentId = profile.StudentId,
                QuestionId = question.QuestionId,
                AttemptId = request.AttemptId,
                EarnedCoins = 0,
                EarnedPoints = 0,
                CoinsDeducted = coinsDeducted,
                IsCorrect = request.GameWon,
                PlayedAt = DateTime.UtcNow
            };

            _db.GameResults.Add(result);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException exception) when (
                exception.InnerException is Npgsql.PostgresException postgresException &&
                postgresException.SqlState == Npgsql.PostgresErrorCodes.UniqueViolation)
            {
                await transaction.RollbackAsync();
                return Conflict("This attempt identifier has already been used.");
            }

            await transaction.CommitAsync();

            return Ok(new MiniGameAttemptResponse
            {
                AttemptId = request.AttemptId,
                GameWon = result.IsCorrect,
                CoinsDeducted = result.CoinsDeducted,
                TotalCoins = profile.TotalCoins,
                AnswerTip = result.IsCorrect ? question.AnswerTip : "",
                AlreadyProcessed = false
            });
        }

        private static bool IsSupportedGameType(string gameType)
        {
            return gameType == "MatchThePairs" ||
                gameType == "FallingDuck" ||
                gameType == "QuickTapChallenge";
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

    public class MiniGameAttemptRequest
    {
        public Guid AttemptId { get; set; }
        public int DeploymentId { get; set; }
        public int QuestionId { get; set; }
        public string GameType { get; set; } = "";
        public bool GameWon { get; set; }
    }

    public class MiniGameAttemptResponse
    {
        public Guid AttemptId { get; set; }
        public bool GameWon { get; set; }
        public int CoinsDeducted { get; set; }
        public int TotalCoins { get; set; }
        public string AnswerTip { get; set; } = "";
        public bool AlreadyProcessed { get; set; }
    }
}
