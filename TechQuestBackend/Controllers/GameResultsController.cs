using System.Data;
using System.Security.Claims;
using System.Text.Json;
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

            var challenge = await _db.NpcChallenges
                .FirstOrDefaultAsync(item => item.StudentId == profile.StudentId &&
                    item.DeploymentId == request.DeploymentId &&
                    item.AttemptId == request.AttemptId);
            if (challenge == null || challenge.QuestionId != request.QuestionId)
                return BadRequest("This mini-game attempt is not assigned to this student challenge.");

            var question = await _db.Questions.AsNoTracking()
                .FirstOrDefaultAsync(item => item.QuestionId == challenge.QuestionId);
            if (question == null)
                return NotFound("NPC question was not found.");

            if (!string.Equals(challenge.MiniGameType, request.GameType, StringComparison.Ordinal))
                return BadRequest("This mini-game is not assigned to the NPC question.");

            if (challenge.State == "QuestionReady" || challenge.State == "Completed")
            {
                if (challenge.MiniGameWon != request.GameWon)
                    return Conflict("This mini-game attempt was already recorded with a different result.");

                var priorResult = await _db.GameResults.AsNoTracking()
                    .FirstOrDefaultAsync(result => result.AttemptId == request.AttemptId);
                await transaction.CommitAsync();
                return Ok(new MiniGameAttemptResponse
                {
                    AttemptId = request.AttemptId,
                    GameWon = challenge.MiniGameWon.Value,
                    CoinsDeducted = priorResult?.CoinsDeducted ?? 0,
                    TotalCoins = profile.TotalCoins,
                    TotalPoints = profile.TotalPoints,
                    AnswerTip = challenge.AnswerTip,
                    AlreadyProcessed = true
                });
            }

            if (challenge.State != "MiniGamePending")
                return Conflict("The NPC challenge is not waiting for a mini-game result.");

            int coinsDeducted = 0;
            if (!request.GameWon)
            {
                coinsDeducted = Math.Min(5, profile.TotalCoins);
                profile.TotalCoins -= coinsDeducted;
            }

            challenge.MiniGameWon = request.GameWon;
            challenge.AnswerTipAwarded = true;
            challenge.State = "QuestionReady";
            var result = new GameResult
            {
                StudentId = profile.StudentId,
                QuestionId = question.QuestionId,
                AttemptId = request.AttemptId,
                EarnedCoins = 0,
                EarnedPoints = 0,
                CoinsDeducted = coinsDeducted,
                PointsDeducted = 0,
                IsCorrect = request.GameWon,
                PlayedAt = DateTime.UtcNow
            };

            _db.GameResults.Add(result);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new MiniGameAttemptResponse
            {
                AttemptId = request.AttemptId,
                GameWon = result.IsCorrect,
                CoinsDeducted = result.CoinsDeducted,
                TotalCoins = profile.TotalCoins,
                TotalPoints = profile.TotalPoints,
                AnswerTip = challenge.AnswerTip,
                AlreadyProcessed = false
            });
        }

        [HttpPost("question-attempt")]
        public async Task<IActionResult> SubmitQuestionAttempt(QuestionAttemptRequest request)
        {
            if (request.DeploymentId <= 0)
                return BadRequest("A valid NPC deployment is required.");

            int userId = GetUserId();
            if (userId <= 0)
                return Unauthorized();

            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            var profile = await _db.StudentProfiles
                .FromSqlInterpolated($"SELECT * FROM \"Student_profile\" WHERE \"UserId\" = {userId} FOR UPDATE")
                .SingleOrDefaultAsync();
            if (profile == null)
                return BadRequest("Student profile was not found.");

            var challenge = await _db.NpcChallenges
                .Include(item => item.Deployment)
                .FirstOrDefaultAsync(item => item.StudentId == profile.StudentId &&
                    item.DeploymentId == request.DeploymentId);
            if (challenge == null)
                return NotFound("NPC challenge was not started.");

            if (challenge.State == "Completed")
            {
                string? completedAnswer = string.IsNullOrWhiteSpace(request.SelectedAnswer)
                    ? null
                    : request.SelectedAnswer.Trim();
                if (!string.Equals(challenge.SelectedAnswer, completedAnswer, StringComparison.Ordinal))
                    return Conflict("This question attempt was already finalized.");

                await transaction.CommitAsync();
                return Ok(CreateQuestionResult(challenge, profile));
            }

            if (challenge.State != "QuestionReady" || challenge.QuestionStartedAt == null)
                return Conflict("The NPC question has not been started.");

            bool timedOut = DateTime.UtcNow >= challenge.QuestionStartedAt.Value.AddSeconds(challenge.TimerSeconds);
            if (!timedOut && string.IsNullOrWhiteSpace(request.SelectedAnswer))
                return BadRequest("Select an answer before submitting.");
            string submittedAnswer = request.SelectedAnswer?.Trim() ?? "";

            if (!timedOut)
            {
                List<string>? choices = JsonSerializer.Deserialize<List<string>>(challenge.AnswerChoices);
                if (choices == null)
                    return BadRequest("The question answer choices are invalid.");
                if (choices.Count > 0 && !choices.Any(choice =>
                    string.Equals(choice, submittedAnswer, StringComparison.OrdinalIgnoreCase)))
                    return BadRequest("The submitted answer is not one of the available choices.");
            }

            string? selectedAnswer = timedOut ? null : submittedAnswer;
            bool isCorrect = !timedOut &&
                string.Equals(selectedAnswer, challenge.CorrectAnswer.Trim(), StringComparison.OrdinalIgnoreCase);
            (int coinReward, int pointReward, int coinPenalty, int pointPenalty) =
                GetQuestionRewards(challenge.Difficulty, isCorrect);
            int coinsDeducted = Math.Min(coinPenalty, profile.TotalCoins);
            int pointsDeducted = Math.Min(pointPenalty, profile.TotalPoints);

            profile.TotalCoins = profile.TotalCoins - coinsDeducted + coinReward;
            profile.TotalPoints = profile.TotalPoints - pointsDeducted + pointReward;
            var leaderboard = await _db.Leaderboards
                .FirstOrDefaultAsync(item => item.StudentId == profile.StudentId);
            if (leaderboard != null)
                leaderboard.TotalPoints = profile.TotalPoints;

            challenge.State = "Completed";
            challenge.CompletedAt = DateTime.UtcNow;
            challenge.SelectedAnswer = selectedAnswer;
            challenge.QuestionCorrect = isCorrect;
            challenge.QuestionTimedOut = timedOut;
            challenge.CoinsChanged = coinReward - coinsDeducted;
            challenge.PointsChanged = pointReward - pointsDeducted;
            if (challenge.Deployment != null)
            {
                challenge.Deployment.Status = "Cancelled";
                challenge.Deployment.PositionX = null;
                challenge.Deployment.PositionY = null;
                challenge.Deployment.PositionMarker = null;
            }

            _db.GameResults.Add(new GameResult
            {
                StudentId = profile.StudentId,
                QuestionId = challenge.QuestionId,
                EarnedCoins = coinReward,
                EarnedPoints = pointReward,
                CoinsDeducted = coinsDeducted,
                PointsDeducted = pointsDeducted,
                IsCorrect = isCorrect,
                PlayedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Ok(CreateQuestionResult(challenge, profile));
        }

        private static QuestionAttemptResponse CreateQuestionResult(
            NpcChallenge challenge,
            StudentProfile profile)
        {
            return new QuestionAttemptResponse
            {
                DeploymentId = challenge.DeploymentId,
                IsCorrect = challenge.QuestionCorrect ?? false,
                TimedOut = challenge.QuestionTimedOut,
                SelectedAnswer = challenge.SelectedAnswer,
                CorrectAnswer = challenge.CorrectAnswer,
                CoinsChanged = challenge.CoinsChanged,
                PointsChanged = challenge.PointsChanged,
                TotalCoins = profile.TotalCoins,
                TotalPoints = profile.TotalPoints,
                Completed = challenge.State == "Completed"
            };
        }

        private static (int Coins, int Points, int CoinPenalty, int PointPenalty) GetQuestionRewards(
            string difficulty,
            bool correct)
        {
            if (string.Equals(difficulty, "Easy", StringComparison.OrdinalIgnoreCase))
                return correct ? (15, 20, 0, 0) : (0, 0, 5, 10);
            if (string.Equals(difficulty, "Medium", StringComparison.OrdinalIgnoreCase))
                return correct ? (20, 30, 0, 0) : (0, 0, 8, 15);
            if (string.Equals(difficulty, "Hard", StringComparison.OrdinalIgnoreCase))
                return correct ? (30, 50, 0, 0) : (0, 0, 10, 20);

            throw new InvalidOperationException("The NPC deployment has an unsupported difficulty.");
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
        public int TotalPoints { get; set; }
        public string AnswerTip { get; set; } = "";
        public bool AlreadyProcessed { get; set; }
    }

    public class QuestionAttemptRequest
    {
        public int DeploymentId { get; set; }
        public string? SelectedAnswer { get; set; }
    }

    public class QuestionAttemptResponse
    {
        public int DeploymentId { get; set; }
        public bool IsCorrect { get; set; }
        public bool TimedOut { get; set; }
        public string? SelectedAnswer { get; set; }
        public string CorrectAnswer { get; set; } = "";
        public int CoinsChanged { get; set; }
        public int PointsChanged { get; set; }
        public int TotalCoins { get; set; }
        public int TotalPoints { get; set; }
        public bool Completed { get; set; }
    }
}
