using System.Data;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechQuestBackend.Data;
using TechQuestBackend.Models;
using TechQuestBackend.Services;

namespace TechQuestBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "student")]
    public class GameResultsController : ControllerBase
    {
        private readonly TechQuestDbContext _db;
        private readonly IQuestProgressService _questProgressService;

        public GameResultsController(TechQuestDbContext db, IQuestProgressService questProgressService)
        {
            _db = db;
            _questProgressService = questProgressService;
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
            await _questProgressService.ApplyEventAsync(
                profile.StudentId,
                $"mini-game:{request.AttemptId:D}",
                "mini_game",
                1,
                miniGameType: challenge.MiniGameType,
                miniGameId: question.MiniGameId,
                successful: request.GameWon);

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
        public async Task<IActionResult> SubmitQuestionAttempt(
            QuestionAttemptRequest request,
            CancellationToken cancellationToken)
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

            bool timedOut = DateTime.UtcNow >= challenge.QuestionStartedAt.Value
                .AddSeconds(challenge.TimeExtensionSeconds > 0
                    ? challenge.TimeExtensionSeconds
                    : challenge.TimerSeconds);
            if (!timedOut && string.IsNullOrWhiteSpace(request.SelectedAnswer) && !request.Finalize)
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

            if (!isCorrect && !request.Finalize)
            {
                challenge.SelectedAnswer = selectedAnswer;
                challenge.QuestionCorrect = false;
                challenge.QuestionTimedOut = timedOut;
                await _db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return Ok(CreateQuestionResult(challenge, profile));
            }

            if (!isCorrect)
            {
                profile.Lifespan = Math.Max(0, profile.Lifespan - 10);
                profile.LifespanAutoRecoveryAt = profile.Lifespan == 0
                    ? DateTime.UtcNow.AddHours(3)
                    : null;
                profile.LifespanWalkRecoveryAt = null;
            }

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

            await _questProgressService.ApplyEventAsync(
                profile.StudentId,
                $"npc-completion:{challenge.ChallengeId}",
                "npc_challenge",
                1,
                npcId: challenge.NpcId);

            await _questProgressService.ApplyEventAsync(
                profile.StudentId,
                $"npc-reward:{challenge.ChallengeId}",
                "coins",
                coinReward);
            await _questProgressService.ApplyEventAsync(
                profile.StudentId,
                $"npc-reward:{challenge.ChallengeId}",
                "points",
                pointReward);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Ok(CreateQuestionResult(challenge, profile));
        }

        [HttpPost("character-challenge")]
        public async Task<IActionResult> SubmitCharacterChallenge(
            CharacterChallengeRequest request,
            CancellationToken cancellationToken)
        {
            if (request.AttemptId == Guid.Empty || request.DeploymentId <= 0 ||
                request.PlayerScore < 0 || request.NpcScore < 0)
                return BadRequest("The character challenge result is invalid.");

            string outcome = (request.Outcome ?? "").Trim().ToUpperInvariant();
            if ((request.PlayerScore > request.NpcScore && outcome != "WIN") ||
                (request.PlayerScore < request.NpcScore && outcome != "LOSE") ||
                (request.PlayerScore == request.NpcScore && outcome != "DRAW"))
                return BadRequest("The challenge outcome does not match the scores.");

            int userId = GetUserId();
            if (userId <= 0)
                return Unauthorized();

            await using var transaction = await _db.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted, cancellationToken);
            var profile = await _db.StudentProfiles
                .FromSqlInterpolated($"SELECT * FROM \"Student_profile\" WHERE \"UserId\" = {userId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (profile == null)
                return BadRequest("Student profile was not found.");

            var challenge = await _db.NpcChallenges
                .FirstOrDefaultAsync(item => item.StudentId == profile.StudentId &&
                    item.DeploymentId == request.DeploymentId && item.AttemptId == request.AttemptId,
                    cancellationToken);
            if (challenge == null || challenge.MiniGameType != "HigherLower")
                return BadRequest("This character challenge is not assigned to the student.");

            if (challenge.State == "CharacterMiniGameCompleted")
            {
                if (challenge.CharacterPlayerScore != request.PlayerScore ||
                    challenge.CharacterNpcScore != request.NpcScore ||
                    challenge.CharacterOutcome != outcome)
                    return Conflict("This character challenge was already finalized with a different result.");

                await transaction.CommitAsync(cancellationToken);
                return Ok(CreateCharacterChallengeResult(challenge, profile, true));
            }

            if (challenge.State != "CharacterMiniGamePending")
                return Conflict("This character challenge is no longer active.");

            int coinsDeducted = outcome == "LOSE" ? Math.Min(20, profile.TotalCoins) : 0;
            int pointsDeducted = outcome == "LOSE" ? Math.Min(20, profile.TotalPoints) : 0;
            profile.TotalCoins -= coinsDeducted;
            profile.TotalPoints -= pointsDeducted;
            challenge.CharacterPlayerScore = request.PlayerScore;
            challenge.CharacterNpcScore = request.NpcScore;
            challenge.CharacterOutcome = outcome;
            challenge.CoinsChanged = -coinsDeducted;
            challenge.PointsChanged = -pointsDeducted;
            challenge.CompletedAt = DateTime.UtcNow;
            challenge.State = "CharacterMiniGameCompleted";

            _db.GameResults.Add(new GameResult
            {
                StudentId = profile.StudentId,
                QuestionId = challenge.QuestionId,
                AttemptId = challenge.AttemptId,
                CoinsDeducted = coinsDeducted,
                PointsDeducted = pointsDeducted,
                IsCorrect = outcome == "WIN",
                PlayedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Ok(CreateCharacterChallengeResult(challenge, profile, false));
        }

        private static CharacterChallengeResultResponse CreateCharacterChallengeResult(
            NpcChallenge challenge,
            StudentProfile profile,
            bool alreadyProcessed)
        {
            return new CharacterChallengeResultResponse
            {
                AttemptId = challenge.AttemptId,
                Outcome = challenge.CharacterOutcome ?? "DRAW",
                PlayerScore = challenge.CharacterPlayerScore,
                NpcScore = challenge.CharacterNpcScore,
                CoinsDeducted = Math.Max(0, -challenge.CoinsChanged),
                PointsDeducted = Math.Max(0, -challenge.PointsChanged),
                TotalCoins = profile.TotalCoins,
                TotalPoints = profile.TotalPoints,
                AlreadyProcessed = alreadyProcessed
            };
        }

        [HttpPost("npc-quiz-purchase")]
        public async Task<IActionResult> PurchaseNpcQuizOption(
            NpcQuizPurchaseRequest request,
            CancellationToken cancellationToken)
        {
            if (request.DeploymentId <= 0 || request.PurchaseId == Guid.Empty)
                return BadRequest("A valid deployment and purchase id are required.");

            string purchaseType = (request.PurchaseType ?? "").Trim().ToLowerInvariant();
            int cost = purchaseType switch
            {
                "extend_time" => 10,
                "extend_time_hint" => 20,
                "hint" => 20,
                _ => 0
            };
            if (cost == 0)
                return BadRequest("The purchase type is invalid.");

            int userId = GetUserId();
            if (userId <= 0)
                return Unauthorized();

            await using var transaction = await _db.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted, cancellationToken);
            var profile = await _db.StudentProfiles
                .FromSqlInterpolated($"SELECT * FROM \"Student_profile\" WHERE \"UserId\" = {userId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (profile == null)
                return BadRequest("Student profile was not found.");

            var challenge = await _db.NpcChallenges
                .FromSqlInterpolated($"SELECT * FROM \"Npc_challenges\" WHERE \"StudentId\" = {profile.StudentId} AND \"DeploymentId\" = {request.DeploymentId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (challenge == null)
                return NotFound("NPC challenge was not found.");
            if (challenge.State != "QuestionReady")
                return Conflict("This NPC question is no longer active.");
            var existingPurchase = await _db.NpcQuizPurchaseTransactions
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.StudentId == profile.StudentId &&
                    item.PurchaseId == request.PurchaseId, cancellationToken);
            if (existingPurchase != null)
            {
                await transaction.CommitAsync(cancellationToken);
                return Ok(CreatePurchaseResponse(challenge, profile));
            }

            if (profile.TotalCoins < cost)
                return BadRequest("Not enough Coins.");

            List<int> hintPositions = JsonSerializer.Deserialize<List<int>>(challenge.HintPositions) ?? new();
            if (purchaseType == "hint" || purchaseType == "extend_time_hint")
            {
                int lettersToReveal = challenge.HintPurchaseCount == 0 ? 2 : 1;
                List<int> eligiblePositions = GetEligibleHintPositions(challenge.CorrectAnswer, hintPositions);
                if (eligiblePositions.Count == 0)
                    return Conflict("All eligible answer letters have already been revealed.");

                for (int i = 0; i < lettersToReveal && eligiblePositions.Count > 0; i++)
                {
                    int selectedIndex = Random.Shared.Next(eligiblePositions.Count);
                    hintPositions.Add(eligiblePositions[selectedIndex]);
                    eligiblePositions.RemoveAt(selectedIndex);
                }
                challenge.HintPurchaseCount++;
                challenge.HintPositions = JsonSerializer.Serialize(hintPositions);
            }

            challenge.QuestionStartedAt = DateTime.UtcNow;
            challenge.TimeExtensionSeconds = 10;

            profile.TotalCoins -= cost;
            _db.NpcQuizPurchaseTransactions.Add(new NpcQuizPurchaseTransaction
            {
                StudentId = profile.StudentId,
                ChallengeId = challenge.ChallengeId,
                PurchaseId = request.PurchaseId,
                PurchaseType = purchaseType,
                CostCoins = cost,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Ok(CreatePurchaseResponse(challenge, profile));
        }

        private static NpcQuizPurchaseResponse CreatePurchaseResponse(
            NpcChallenge challenge,
            StudentProfile profile)
        {
            List<int> positions = JsonSerializer.Deserialize<List<int>>(challenge.HintPositions) ?? new();
            return new NpcQuizPurchaseResponse
            {
                TotalCoins = profile.TotalCoins,
                TimeExtensionSeconds = challenge.TimeExtensionSeconds,
                HintDisplay = BuildHintDisplay(challenge.CorrectAnswer, positions),
                HintPositions = positions,
                HintPurchaseCount = challenge.HintPurchaseCount,
                CanPurchaseHint = GetEligibleHintPositions(challenge.CorrectAnswer, positions).Count > 0
            };
        }

        private static List<int> GetEligibleHintPositions(string answer, List<int> revealedPositions)
        {
            List<int> positions = new();
            for (int index = 0; index < answer.Length; index++)
            {
                if (!char.IsLetterOrDigit(answer[index]) || revealedPositions.Contains(index))
                    continue;
                positions.Add(index);
            }
            return positions;
        }

        private static string BuildHintDisplay(string answer, List<int> revealedPositions)
        {
            char[] display = new char[answer.Length];
            for (int index = 0; index < answer.Length; index++)
                display[index] = char.IsLetterOrDigit(answer[index])
                    ? revealedPositions.Contains(index) ? answer[index] : '_'
                    : answer[index];
            return new string(display);
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
                CorrectAnswer = challenge.State == "Completed" ? challenge.CorrectAnswer : "",
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

    public class CharacterChallengeRequest
    {
        public Guid AttemptId { get; set; }
        public int DeploymentId { get; set; }
        public int PlayerScore { get; set; }
        public int NpcScore { get; set; }
        public string Outcome { get; set; } = "";
    }

    public class CharacterChallengeResultResponse
    {
        public Guid AttemptId { get; set; }
        public int PlayerScore { get; set; }
        public int NpcScore { get; set; }
        public string Outcome { get; set; } = "";
        public int CoinsDeducted { get; set; }
        public int PointsDeducted { get; set; }
        public int TotalCoins { get; set; }
        public int TotalPoints { get; set; }
        public bool AlreadyProcessed { get; set; }
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
        public bool Finalize { get; set; }
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

    public class NpcQuizPurchaseRequest
    {
        public int DeploymentId { get; set; }
        public Guid PurchaseId { get; set; }
        public string PurchaseType { get; set; } = "";
    }

    public class NpcQuizPurchaseResponse
    {
        public int TotalCoins { get; set; }
        public int TimeExtensionSeconds { get; set; }
        public string HintDisplay { get; set; } = "";
        public List<int> HintPositions { get; set; } = new();
        public int HintPurchaseCount { get; set; }
        public bool CanPurchaseHint { get; set; }
    }
}
