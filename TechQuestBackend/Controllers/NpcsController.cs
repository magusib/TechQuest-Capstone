using System.Security.Claims;
using System.Data;
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
    [Authorize]
    public class NpcsController : ControllerBase
    {
        private readonly TechQuestDbContext _db;
        private readonly IWebHostEnvironment _environment;

        public NpcsController(TechQuestDbContext db, IWebHostEnvironment environment)
        {
            _db = db;
            _environment = environment;
        }

        [HttpGet]
        [Authorize(Roles = "student,admin")]
        public async Task<IActionResult> Get([FromQuery] string? difficulty, CancellationToken cancellationToken)
        {
            var query = _db.Npcs.AsNoTracking().Where(n => n.Status == "active");
            if (!string.IsNullOrWhiteSpace(difficulty))
                query = query.Where(n => n.Difficulty.ToLower() == difficulty.Trim().ToLower());

            return Ok(await query.OrderBy(n => n.NpcId).ToListAsync(cancellationToken));
        }

        [HttpGet("{npcId:int}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetById(int npcId, CancellationToken cancellationToken)
        {
            var npc = await _db.Npcs.AsNoTracking()
                .FirstOrDefaultAsync(n => n.NpcId == npcId, cancellationToken);
            return npc == null ? NotFound() : Ok(npc);
        }

        [HttpGet("{npcId:int}/configuration")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetConfiguration(int npcId, CancellationToken cancellationToken)
        {
            var npc = await _db.Npcs.AsNoTracking()
                .FirstOrDefaultAsync(n => n.NpcId == npcId && n.Status == "active", cancellationToken);
            if (npc == null)
                return NotFound("NPC was not found or is inactive.");

            var question = await _db.Questions.AsNoTracking()
                .Where(q => q.NpcId == npcId)
                .Where(q => q.DeploymentId == null)
                .OrderBy(q => q.QuestionId)
                .Select(q => new NpcConfigurationResponse
                {
                    NpcId = npcId,
                    SubjectName = q.SubjectName,
                    MiniGameId = q.MiniGameId,
                    MiniGameName = q.MiniGame == null ? "" : q.MiniGame.GameName,
                    Question = q.QuestionText,
                    CorrectAnswer = q.CorrectAnswer,
                    AnswerTip = q.AnswerTip,
                    TimerSeconds = q.TimerSeconds
                })
                .FirstOrDefaultAsync(cancellationToken);

            return Ok(question ?? new NpcConfigurationResponse { NpcId = npcId });
        }

        [HttpGet("deployments/{deploymentId:int}/configuration")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetDeploymentConfiguration(
            int deploymentId,
            CancellationToken cancellationToken)
        {
            var deployment = await _db.NpcDeployments.AsNoTracking()
                .Include(item => item.Npc)
                .FirstOrDefaultAsync(
                    item => item.DeploymentId == deploymentId &&
                        (item.Status == "Deployed" || item.Status == "Pending") &&
                        item.Npc != null &&
                        item.Npc.Status == "active",
                    cancellationToken);
            if (deployment == null)
                return NotFound("Active NPC deployment was not found.");

            var configuration = await _db.Questions.AsNoTracking()
                .Where(question => question.DeploymentId == deploymentId)
                .Select(question => new NpcConfigurationResponse
                {
                    NpcId = question.NpcId ?? 0,
                    DeploymentId = question.DeploymentId,
                    SubjectName = question.SubjectName,
                    MiniGameId = question.MiniGameId,
                    MiniGameName = question.MiniGame == null ? "" : question.MiniGame.GameName,
                    Question = question.QuestionText,
                    AnswerChoices = JsonSerializer.Deserialize<List<string>>(question.AnswerChoices) ?? new List<string>(),
                    CorrectAnswer = question.CorrectAnswer,
                    AnswerTip = question.AnswerTip,
                    TimerSeconds = question.TimerSeconds
                })
                .FirstOrDefaultAsync(cancellationToken);

            return Ok(configuration ?? new NpcConfigurationResponse
            {
                NpcId = deployment.NpcId,
                DeploymentId = deploymentId
            });
        }

        [HttpPut("deployments/{deploymentId:int}/configuration")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateDeploymentConfiguration(
            int deploymentId,
            NpcConfigurationRequest request,
            CancellationToken cancellationToken)
        {
            var deployment = await _db.NpcDeployments
                .Include(item => item.Npc)
                .FirstOrDefaultAsync(
                    item => item.DeploymentId == deploymentId &&
                        (item.Status == "Deployed" || item.Status == "Pending") &&
                        item.Npc != null &&
                        item.Npc.Status == "active",
                    cancellationToken);
            if (deployment == null)
                return NotFound("Active NPC deployment was not found.");

            if (string.IsNullOrWhiteSpace(request.SubjectName) ||
                string.IsNullOrWhiteSpace(request.Question) ||
                string.IsNullOrWhiteSpace(request.CorrectAnswer))
                return BadRequest("Subject, question, and correct answer are required.");
            if (request.AnswerChoices != null &&
                !HasValidAnswerChoices(JsonSerializer.Serialize(request.AnswerChoices), request.CorrectAnswer))
                return BadRequest("If answer choices are provided, include at least two choices and the correct answer.");

            if (request.TimerSeconds < 1)
                return BadRequest("Timer must be at least one second.");

            if (request.MiniGameId.HasValue &&
                !await _db.MiniGames.AnyAsync(
                    game => game.MiniGameId == request.MiniGameId.Value,
                    cancellationToken))
                return BadRequest("Mini-game was not found.");

            var question = await _db.Questions
                .FirstOrDefaultAsync(item => item.DeploymentId == deploymentId, cancellationToken);
            if (question == null)
            {
                question = new Question
                {
                    NpcId = deployment.NpcId,
                    DeploymentId = deploymentId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = GetUserId()
                };
                _db.Questions.Add(question);
            }

            question.SubjectName = request.SubjectName.Trim();
            question.MiniGameId = request.MiniGameId;
            question.QuestionText = request.Question.Trim();
            question.AnswerChoices = JsonSerializer.Serialize(
                NormalizeAnswerChoices(request.AnswerChoices ?? new List<string>()));
            question.CorrectAnswer = request.CorrectAnswer.Trim();
            question.AnswerTip = request.AnswerTip?.Trim() ?? "";
            question.TimerSeconds = request.TimerSeconds;
            await _db.SaveChangesAsync(cancellationToken);

            return Ok(new NpcConfigurationResponse
            {
                NpcId = deployment.NpcId,
                DeploymentId = deploymentId,
                SubjectName = question.SubjectName,
                MiniGameId = question.MiniGameId,
                MiniGameName = question.MiniGameId.HasValue
                    ? await _db.MiniGames
                        .Where(game => game.MiniGameId == question.MiniGameId.Value)
                        .Select(game => game.GameName)
                        .FirstOrDefaultAsync(cancellationToken) ?? ""
                    : "",
                Question = question.QuestionText,
                AnswerChoices = JsonSerializer.Deserialize<List<string>>(question.AnswerChoices) ?? new List<string>(),
                CorrectAnswer = question.CorrectAnswer,
                AnswerTip = question.AnswerTip,
                TimerSeconds = question.TimerSeconds
            });
        }

        private async Task<NpcDeployment?> FindActiveDeployment(int deploymentId, CancellationToken cancellationToken)
        {
            return await _db.NpcDeployments.AsNoTracking()
                .Include(deployment => deployment.Npc)
                .FirstOrDefaultAsync(
                    deployment => deployment.DeploymentId == deploymentId &&
                        deployment.Status == "Deployed" &&
                        deployment.Npc != null &&
                        deployment.Npc.Status == "active",
                    cancellationToken);
        }

        [HttpGet("deployments/{deploymentId:int}/student-game")]
        [Authorize(Roles = "student")]
        public async Task<IActionResult> GetStudentGame(int deploymentId, CancellationToken cancellationToken)
        {
            var deployment = await FindActiveDeployment(deploymentId, cancellationToken);
            if (deployment == null)
                return NotFound("NPC deployment was not found or is no longer active.");

            var assignment = await _db.Questions.AsNoTracking()
                .Where(question => question.DeploymentId == deploymentId && question.MiniGameId.HasValue)
                .OrderBy(question => question.QuestionId)
                .Select(question => new
                {
                    question.QuestionId,
                    question.NpcId,
                    question.DeploymentId,
                    question.MiniGame!.GameType,
                    question.MiniGame.GameName,
                    TimerSeconds = 30
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (assignment == null)
                return NotFound("This NPC does not have a mini-game assignment.");

            if (assignment.GameType != "MatchThePairs" &&
                assignment.GameType != "FallingDuck" &&
                assignment.GameType != "QuickTapChallenge")
                return BadRequest("This NPC has an unsupported mini-game assignment.");

            return Ok(assignment);
        }

        [HttpGet("deployments/{deploymentId:int}/student-configuration")]
        [Authorize(Roles = "student")]
        public async Task<IActionResult> GetStudentConfiguration(int deploymentId, CancellationToken cancellationToken)
        {
            var deployment = await FindActiveDeployment(deploymentId, cancellationToken);
            if (deployment == null)
                return NotFound("NPC deployment was not found or is no longer active.");

            var question = await _db.Questions.AsNoTracking()
                .Include(item => item.MiniGame)
                .Where(item => item.DeploymentId == deploymentId)
                .Select(item => new
                {
                    item.QuestionId,
                    item.NpcId,
                    item.SubjectName,
                    item.MiniGameId,
                    MiniGameName = item.MiniGame == null ? "" : item.MiniGame.GameName,
                    TimerSeconds = item.TimerSeconds
                })
                .FirstOrDefaultAsync(cancellationToken);
            if (question == null)
                return NotFound("Question configuration was not found.");

            int userId = GetUserId();
            var existingChallenge = await _db.NpcChallenges.AsNoTracking()
                .Where(challenge => challenge.Student!.UserId == userId &&
                    challenge.DeploymentId == deploymentId &&
                    challenge.MiniGameType != "HigherLower")
                .Select(challenge => new
                {
                    challenge.State,
                    challenge.AnswerTip
                })
                .FirstOrDefaultAsync(cancellationToken);

            return Ok(new StudentNpcConfigurationResponse
            {
                NpcId = deployment.NpcId,
                NpcName = deployment.Npc?.NpcName ?? "",
                DeploymentId = deployment.DeploymentId,
                Difficulty = deployment.MapDifficulty,
                PositionMarker = deployment.PositionMarker,
                QuestionId = question.QuestionId,
                SubjectName = question.SubjectName,
                MiniGameId = question.MiniGameId,
                MiniGameName = question.MiniGameName,
                TimerSeconds = question.TimerSeconds,
                AlreadyStarted = existingChallenge != null,
                ChallengeState = existingChallenge?.State ?? "",
                AnswerTip = existingChallenge?.State == "QuestionReady"
                    ? existingChallenge.AnswerTip
                    : ""
            });
        }

        [HttpPost("deployments/{deploymentId:int}/challenge-start")]
        [Authorize(Roles = "student")]
        public async Task<IActionResult> StartStudentChallenge(int deploymentId, CancellationToken cancellationToken)
        {
            int userId = GetUserId();
            if (userId <= 0)
                return Unauthorized();

            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
            var profile = await _db.StudentProfiles
                .FromSqlInterpolated($"SELECT * FROM \"Student_profile\" WHERE \"UserId\" = {userId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (profile == null)
                return BadRequest("Student profile was not found.");

            if (profile.Lifespan <= 0)
                return StatusCode(StatusCodes.Status403Forbidden,
                    "You have no remaining Lifespan. Restore it before challenging an NPC.");

            var deployment = await _db.NpcDeployments
                .Include(item => item.Npc)
                .FirstOrDefaultAsync(item => item.DeploymentId == deploymentId &&
                    item.Status == "Deployed" &&
                    item.Npc != null &&
                    item.Npc.Status == "active", cancellationToken);
            if (deployment == null)
                return NotFound("NPC deployment was not found or is no longer active.");

            var existingChallenge = await _db.NpcChallenges
                .FirstOrDefaultAsync(challenge => challenge.StudentId == profile.StudentId &&
                    challenge.DeploymentId == deploymentId, cancellationToken);
            if (existingChallenge != null)
            {
                if (existingChallenge.MiniGameType == "HigherLower")
                {
                    var replacementQuestion = await _db.Questions
                        .Include(item => item.MiniGame)
                        .FirstOrDefaultAsync(item => item.DeploymentId == deploymentId, cancellationToken);
                    if (replacementQuestion == null ||
                        replacementQuestion.MiniGame == null ||
                        !IsSupportedMiniGameType(replacementQuestion.MiniGame.GameType) ||
                        string.IsNullOrWhiteSpace(replacementQuestion.SubjectName) ||
                        string.IsNullOrWhiteSpace(replacementQuestion.QuestionText) ||
                        string.IsNullOrWhiteSpace(replacementQuestion.CorrectAnswer) ||
                        replacementQuestion.TimerSeconds < 1 ||
                        !HasValidAnswerChoices(replacementQuestion.AnswerChoices, replacementQuestion.CorrectAnswer))
                        return BadRequest("The NPC challenge is not fully configured.");

                    existingChallenge.QuestionId = replacementQuestion.QuestionId;
                    existingChallenge.NpcId = deployment.NpcId;
                    existingChallenge.NpcName = deployment.Npc!.NpcName;
                    existingChallenge.Difficulty = deployment.MapDifficulty;
                    existingChallenge.SubjectName = replacementQuestion.SubjectName;
                    existingChallenge.MiniGameType = replacementQuestion.MiniGame.GameType;
                    existingChallenge.QuestionText = replacementQuestion.QuestionText;
                    existingChallenge.AnswerChoices = replacementQuestion.AnswerChoices;
                    existingChallenge.CorrectAnswer = replacementQuestion.CorrectAnswer;
                    existingChallenge.AnswerTip = replacementQuestion.AnswerTip;
                    existingChallenge.TimerSeconds = replacementQuestion.TimerSeconds;
                    existingChallenge.AttemptId = Guid.NewGuid();
                    existingChallenge.State = "MiniGamePending";
                    existingChallenge.StartedAt = DateTime.UtcNow;
                    existingChallenge.QuestionStartedAt = null;
                    existingChallenge.CompletedAt = null;
                    existingChallenge.MiniGameWon = null;
                    existingChallenge.AnswerTipAwarded = false;
                    existingChallenge.SelectedAnswer = null;
                    existingChallenge.QuestionCorrect = null;
                    existingChallenge.QuestionTimedOut = false;
                    existingChallenge.CoinsChanged = 0;
                    existingChallenge.PointsChanged = 0;
                    existingChallenge.CharacterName = null;
                    existingChallenge.CharacterPlayerScore = 0;
                    existingChallenge.CharacterNpcScore = 0;
                    existingChallenge.CharacterOutcome = null;
                    await _db.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return Ok(new StudentChallengeStartResponse
                    {
                        ChallengeId = existingChallenge.ChallengeId,
                        AttemptId = existingChallenge.AttemptId,
                        NpcId = existingChallenge.NpcId,
                        DeploymentId = existingChallenge.DeploymentId,
                        QuestionId = existingChallenge.QuestionId,
                        GameType = existingChallenge.MiniGameType,
                        TimerSeconds = existingChallenge.TimerSeconds,
                        Difficulty = existingChallenge.Difficulty
                    });
                }

                if (existingChallenge.State != "MiniGamePending")
                    return Conflict("This student has already completed the mini-game for this NPC challenge.");

                await transaction.CommitAsync(cancellationToken);
                return Ok(new StudentChallengeStartResponse
                {
                    ChallengeId = existingChallenge.ChallengeId,
                    AttemptId = existingChallenge.AttemptId,
                    NpcId = existingChallenge.NpcId,
                    DeploymentId = existingChallenge.DeploymentId,
                    QuestionId = existingChallenge.QuestionId,
                    GameType = existingChallenge.MiniGameType,
                    TimerSeconds = 30,
                    Difficulty = existingChallenge.Difficulty
                });
            }

            var question = await _db.Questions
                .Include(item => item.MiniGame)
                .FirstOrDefaultAsync(item => item.DeploymentId == deploymentId, cancellationToken);
            if (question == null ||
                question.MiniGame == null ||
                !IsSupportedMiniGameType(question.MiniGame.GameType) ||
                string.IsNullOrWhiteSpace(question.SubjectName) ||
                string.IsNullOrWhiteSpace(question.QuestionText) ||
                string.IsNullOrWhiteSpace(question.CorrectAnswer) ||
                question.TimerSeconds < 1 ||
                !HasValidAnswerChoices(question.AnswerChoices, question.CorrectAnswer))
                return BadRequest("The NPC challenge is not fully configured.");

            var challenge = new NpcChallenge
            {
                StudentId = profile.StudentId,
                DeploymentId = deploymentId,
                QuestionId = question.QuestionId,
                NpcId = deployment.NpcId,
                NpcName = deployment.Npc!.NpcName,
                Difficulty = deployment.MapDifficulty,
                SubjectName = question.SubjectName,
                MiniGameType = question.MiniGame.GameType,
                QuestionText = question.QuestionText,
                AnswerChoices = question.AnswerChoices,
                CorrectAnswer = question.CorrectAnswer,
                AnswerTip = question.AnswerTip,
                TimerSeconds = question.TimerSeconds,
                AttemptId = Guid.NewGuid(),
                StartedAt = DateTime.UtcNow
            };
            _db.NpcChallenges.Add(challenge);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Ok(new StudentChallengeStartResponse
            {
                ChallengeId = challenge.ChallengeId,
                AttemptId = challenge.AttemptId,
                NpcId = deployment.NpcId,
                DeploymentId = deployment.DeploymentId,
                QuestionId = question.QuestionId,
                GameType = challenge.MiniGameType,
                TimerSeconds = 30,
                Difficulty = deployment.MapDifficulty
            });
        }

        [HttpPost("deployments/{deploymentId:int}/character-challenge-start")]
        [Authorize(Roles = "student")]
        public async Task<IActionResult> StartCharacterChallenge(
            int deploymentId,
            CharacterChallengeStartRequest request,
            CancellationToken cancellationToken)
        {
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
            if (profile.Lifespan <= 0)
                return StatusCode(StatusCodes.Status403Forbidden,
                    "You have no remaining Lifespan. Restore it before challenging an NPC.");

            var deployment = await _db.NpcDeployments
                .Include(item => item.Npc)
                .FirstOrDefaultAsync(item => item.DeploymentId == deploymentId &&
                    item.Status == "Deployed" && item.Npc != null && item.Npc.Status == "active",
                    cancellationToken);
            if (deployment == null)
                return NotFound("NPC deployment was not found or is no longer active.");

            var activeSession = await _db.CharacterChallengeSessions
                .FirstOrDefaultAsync(item => item.StudentId == profile.StudentId &&
                    item.DeploymentId == deploymentId && item.State == "Pending", cancellationToken);
            if (activeSession != null)
            {
                await transaction.CommitAsync(cancellationToken);
                return Ok(CreateCharacterChallengeStartResponse(activeSession));
            }

            var question = await _db.Questions
                .FirstOrDefaultAsync(item => item.DeploymentId == deploymentId, cancellationToken);
            if (question == null)
                return BadRequest("The NPC challenge is not fully configured.");

            var session = new CharacterChallengeSession
            {
                StudentId = profile.StudentId,
                DeploymentId = deploymentId,
                NpcId = deployment.NpcId,
                NpcName = deployment.Npc!.NpcName,
                AttemptId = Guid.NewGuid(),
                CharacterName = string.IsNullOrWhiteSpace(request.CharacterName)
                    ? "Character"
                    : request.CharacterName.Trim(),
                State = "Pending"
            };
            _db.CharacterChallengeSessions.Add(session);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Ok(CreateCharacterChallengeStartResponse(session));
        }

        private static CharacterChallengeStartResponse CreateCharacterChallengeStartResponse(
            CharacterChallengeSession session)
        {
            return new CharacterChallengeStartResponse
            {
                ChallengeId = session.CharacterChallengeSessionId,
                AttemptId = session.AttemptId,
                NpcId = session.NpcId,
                NpcName = session.NpcName,
                DeploymentId = session.DeploymentId,
                CharacterName = session.CharacterName,
                GameType = "HigherLower",
                State = session.State == "Completed"
                    ? "CharacterMiniGameCompleted"
                    : "CharacterMiniGamePending"
            };
        }

        [HttpGet("deployments/{deploymentId:int}/student-question")]
        [Authorize(Roles = "student")]
        public async Task<IActionResult> GetStudentQuestion(int deploymentId, CancellationToken cancellationToken)
        {
            int userId = GetUserId();
            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
            var challenge = await _db.NpcChallenges
                .FromSqlInterpolated($@"SELECT c.* FROM ""Npc_challenges"" c
                    JOIN ""Student_profile"" s ON s.""StudentId"" = c.""StudentId""
                    WHERE s.""UserId"" = {userId} AND c.""DeploymentId"" = {deploymentId}
                    FOR UPDATE OF c")
                .SingleOrDefaultAsync(cancellationToken);
            if (challenge == null)
                return NotFound("NPC challenge was not started.");
            if (challenge.State == "Completed")
                return Conflict("This NPC challenge is already complete.");
            if (challenge.State != "QuestionReady")
                return Conflict("Complete the assigned mini-game before opening the question.");

            if (challenge.QuestionStartedAt == null)
            {
                challenge.QuestionStartedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
            }

            DateTime questionDeadline = challenge.QuestionStartedAt.Value
                .AddSeconds(challenge.TimeExtensionSeconds > 0
                    ? challenge.TimeExtensionSeconds
                    : challenge.TimerSeconds);
            var question = new StudentChallengeQuestionResponse
            {
                QuestionId = challenge.QuestionId,
                NpcId = challenge.NpcId,
                DeploymentId = challenge.DeploymentId,
                SubjectName = challenge.SubjectName,
                Question = challenge.QuestionText,
                AnswerChoices = JsonSerializer.Deserialize<List<string>>(challenge.AnswerChoices) ?? new List<string>(),
                TimerSeconds = challenge.TimerSeconds,
                QuestionStartedAt = challenge.QuestionStartedAt.Value,
                RemainingSeconds = Math.Max(0, (int)Math.Ceiling((questionDeadline - DateTime.UtcNow).TotalSeconds)),
                Difficulty = challenge.Difficulty
            };

            await transaction.CommitAsync(cancellationToken);
            return Ok(question);
        }

        [HttpPut("{npcId:int}/configuration")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateConfiguration(
            int npcId,
            NpcConfigurationRequest request,
            CancellationToken cancellationToken)
        {
            var npc = await _db.Npcs
                .FirstOrDefaultAsync(n => n.NpcId == npcId && n.Status == "active", cancellationToken);
            if (npc == null)
                return NotFound("NPC was not found or is inactive.");

            if (string.IsNullOrWhiteSpace(request.SubjectName) ||
                string.IsNullOrWhiteSpace(request.Question) ||
                string.IsNullOrWhiteSpace(request.CorrectAnswer))
                return BadRequest("Subject, question, and correct answer are required.");
            if (request.AnswerChoices != null &&
                !HasValidAnswerChoices(JsonSerializer.Serialize(request.AnswerChoices), request.CorrectAnswer))
                return BadRequest("If answer choices are provided, include at least two choices and the correct answer.");

            if (request.TimerSeconds < 1)
                return BadRequest("Timer must be at least one second.");

            if (request.MiniGameId.HasValue &&
                !await _db.MiniGames.AnyAsync(m => m.MiniGameId == request.MiniGameId.Value, cancellationToken))
                return BadRequest("Mini-game was not found.");

            var question = await _db.Questions
                .Where(q => q.NpcId == npcId && q.DeploymentId == null)
                .OrderBy(q => q.QuestionId)
                .FirstOrDefaultAsync(cancellationToken);

            if (question == null)
            {
                question = new Question
                {
                    NpcId = npcId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = GetUserId()
                };
                _db.Questions.Add(question);
            }

            question.SubjectName = request.SubjectName.Trim();
            question.MiniGameId = request.MiniGameId;
            question.QuestionText = request.Question.Trim();
            question.AnswerChoices = JsonSerializer.Serialize(
                NormalizeAnswerChoices(request.AnswerChoices ?? new List<string>()));
            question.CorrectAnswer = request.CorrectAnswer.Trim();
            question.AnswerTip = request.AnswerTip?.Trim() ?? "";
            question.TimerSeconds = request.TimerSeconds;

            await _db.SaveChangesAsync(cancellationToken);
            return Ok(new NpcConfigurationResponse
            {
                NpcId = npcId,
                SubjectName = question.SubjectName,
                MiniGameId = question.MiniGameId,
                MiniGameName = question.MiniGameId.HasValue
                    ? await _db.MiniGames.Where(m => m.MiniGameId == question.MiniGameId.Value)
                        .Select(m => m.GameName).FirstOrDefaultAsync(cancellationToken) ?? ""
                    : "",
                Question = question.QuestionText,
                AnswerChoices = JsonSerializer.Deserialize<List<string>>(question.AnswerChoices) ?? new List<string>(),
                CorrectAnswer = question.CorrectAnswer,
                AnswerTip = question.AnswerTip,
                TimerSeconds = question.TimerSeconds
            });
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create(NpcRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.NpcName))
                return BadRequest("NPC name is required.");
            if (!IsDifficulty(request.Difficulty))
                return BadRequest("Difficulty must be Easy, Medium, or Hard.");
            if (string.IsNullOrWhiteSpace(request.NpcImage) &&
                string.IsNullOrWhiteSpace(request.AnimationScenePath))
                return BadRequest("Choose a local NPC image or an animation scene.");
            if (!string.IsNullOrWhiteSpace(request.NpcImage) &&
                !HasValidImagePath(request.NpcImage))
                return BadRequest("NPC image must be a local asset under res://Items/NPC.");
            if (!IsValidAnimationScenePath(request.AnimationScenePath, request.Difficulty))
                return BadRequest("NPC animation scene must be a scene in the matching difficulty NPC folder.");
            string npcName = request.NpcName.Trim();
            bool nameExists = await _db.Npcs.AnyAsync(
                n => n.NpcName.ToLower() == npcName.ToLower(), cancellationToken);
            if (nameExists)
                return Conflict("NPC names must be unique so the same NPC is not added to multiple difficulty levels.");

            var npc = new Npc
            {
                NpcName = npcName,
                NpcImage = request.NpcImage,
                AnimationScenePath = NormalizeAnimationScenePath(request.AnimationScenePath),
                Difficulty = NormalizeDifficulty(request.Difficulty),
                Status = string.IsNullOrWhiteSpace(request.Status) ? "active" : request.Status.Trim(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = GetUserId()
            };

            _db.Npcs.Add(npc);
            await _db.SaveChangesAsync(cancellationToken);
            return CreatedAtAction(nameof(GetById), new { npcId = npc.NpcId }, npc);
        }

        [HttpPut("{npcId:int}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Update(int npcId, NpcRequest request, CancellationToken cancellationToken)
        {
            var npc = await _db.Npcs.FindAsync(new object[] { npcId }, cancellationToken);
            if (npc == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(request.NpcName))
                return BadRequest("NPC name is required.");
            if (!IsDifficulty(request.Difficulty))
                return BadRequest("Difficulty must be Easy, Medium, or Hard.");
            if (!IsValidAnimationScenePath(request.AnimationScenePath, request.Difficulty))
                return BadRequest("NPC animation scene must be a scene in the matching difficulty NPC folder.");
            string npcName = request.NpcName.Trim();
            bool nameExists = await _db.Npcs.AnyAsync(
                n => n.NpcId != npcId && n.NpcName.ToLower() == npcName.ToLower(),
                cancellationToken);
            if (nameExists)
                return Conflict("NPC names must be unique so the same NPC is not added to multiple difficulty levels.");
            if (!string.IsNullOrWhiteSpace(request.NpcImage))
            {
                if (!HasValidImagePath(request.NpcImage))
                    return BadRequest("NPC image must be a local asset under res://Items/NPC.");
                npc.NpcImage = request.NpcImage;
            }

            npc.NpcName = npcName;
            if (request.AnimationScenePath != null)
                npc.AnimationScenePath = NormalizeAnimationScenePath(request.AnimationScenePath);
            npc.Difficulty = NormalizeDifficulty(request.Difficulty);
            npc.Status = string.IsNullOrWhiteSpace(request.Status) ? npc.Status : request.Status.Trim();
            await _db.SaveChangesAsync(cancellationToken);
            return Ok(npc);
        }

        [HttpDelete("{npcId:int}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int npcId, CancellationToken cancellationToken)
        {
            var npc = await _db.Npcs.FindAsync(new object[] { npcId }, cancellationToken);
            if (npc == null)
                return NotFound();

            npc.Status = "inactive";
            var deployments = await _db.NpcDeployments
                .Where(d => d.NpcId == npcId &&
                    (d.Status == "Deployed" || d.Status == "Pending"))
                .ToListAsync(cancellationToken);
            foreach (var deployment in deployments)
            {
                deployment.Status = "Cancelled";
                deployment.PositionX = null;
                deployment.PositionY = null;
                deployment.PositionMarker = null;
            }

            await _db.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        [HttpPost("image")]
        [Authorize(Roles = "admin")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> UploadImage(IFormFile image, CancellationToken cancellationToken)
        {
            if (image == null || image.Length == 0)
                return BadRequest("Select an NPC image.");

            string extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (extension is not ".png" and not ".jpg" and not ".jpeg")
                return BadRequest("NPC image must be PNG, JPG, or JPEG.");

            byte[] header = new byte[8];
            if (image.Length < header.Length)
                return BadRequest("The selected file is not a valid PNG or JPEG image.");

            await using (var stream = image.OpenReadStream())
                await stream.ReadExactlyAsync(header, cancellationToken);

            bool validPng = extension == ".png" &&
                header[0] == 137 && header[1] == 80 && header[2] == 78 && header[3] == 71;
            bool validJpeg = (extension == ".jpg" || extension == ".jpeg") &&
                header[0] == 255 && header[1] == 216 && header[2] == 255;
            if (!validPng && !validJpeg)
                return BadRequest("The selected file is not a valid PNG or JPEG image.");

            int userId = GetUserId();
            string uploadFolder = NpcImageStorage.GetFolder(_environment);

            string fileName = $"{userId}_{Guid.NewGuid():N}{extension}";
            string filePath = Path.Combine(uploadFolder, fileName);
            await using (var fileStream = System.IO.File.Create(filePath))
                await image.CopyToAsync(fileStream, cancellationToken);

            return Ok(new { npcImage = $"/uploads/npcs/{fileName}" });
        }

        [HttpPost("{npcId:int}/deployments")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Deploy(int npcId, NpcDeploymentRequest request, CancellationToken cancellationToken)
        {
            if (!IsDifficulty(request.MapDifficulty))
                return BadRequest("Map difficulty must be Easy, Medium, or Hard.");
            bool hasMarker = !string.IsNullOrWhiteSpace(request.PositionMarker);
            if (hasMarker != (request.PositionX.HasValue && request.PositionY.HasValue) ||
                request.PositionX.HasValue != request.PositionY.HasValue ||
                (request.PositionX.HasValue &&
                    (!float.IsFinite(request.PositionX.Value) || !float.IsFinite(request.PositionY!.Value))))
                return BadRequest("A spawn marker and both coordinates must be provided together.");

            string difficulty = NormalizeDifficulty(request.MapDifficulty);
            var npc = await _db.Npcs.FirstOrDefaultAsync(
                n => n.NpcId == npcId && n.Status == "active", cancellationToken);
            if (npc == null)
                return NotFound("NPC was not found or is inactive.");
            if (!string.Equals(npc.Difficulty, difficulty, StringComparison.OrdinalIgnoreCase))
                return BadRequest("NPC difficulty must match the map difficulty.");
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            await LockDeploymentQueueAsync(difficulty, cancellationToken);

            string? positionMarker = hasMarker ? request.PositionMarker!.Trim() : null;
            bool spawnPointOccupied = positionMarker != null &&
                await _db.NpcDeployments.AnyAsync(
                    deployment => deployment.MapDifficulty == difficulty &&
                        deployment.PositionMarker == positionMarker &&
                        deployment.Status == "Deployed" &&
                        !_db.NpcChallenges.Any(challenge =>
                            challenge.DeploymentId == deployment.DeploymentId &&
                            challenge.State == "Completed"),
                    cancellationToken);

            var deployment = new NpcDeployment
            {
                NpcId = npcId,
                MapDifficulty = difficulty,
                PositionX = spawnPointOccupied || positionMarker == null ? null : request.PositionX,
                PositionY = spawnPointOccupied || positionMarker == null ? null : request.PositionY,
                PositionMarker = spawnPointOccupied ? null : positionMarker,
                Status = positionMarker != null && !spawnPointOccupied ? "Deployed" : "Pending",
                DeployedBy = GetUserId(),
                DeployedAt = DateTime.UtcNow
            };
            _db.NpcDeployments.Add(deployment);
            await _db.SaveChangesAsync(cancellationToken);

            _db.Questions.Add(new Question
            {
                NpcId = npcId,
                DeploymentId = deployment.DeploymentId,
                MiniGameId = null,
                SubjectName = "",
                QuestionText = "",
                CorrectAnswer = "",
                AnswerTip = "",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = GetUserId()
            });

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Ok(ToDeploymentResponse(deployment, npc));
        }

        [HttpGet("deployments")]
        [Authorize(Roles = "student,admin")]
        public async Task<IActionResult> GetDeployments([FromQuery] string difficulty, CancellationToken cancellationToken)
        {
            if (!IsDifficulty(difficulty))
                return BadRequest("Difficulty must be Easy, Medium, or Hard.");

            string mapDifficulty = NormalizeDifficulty(difficulty);
            var deploymentQuery = _db.NpcDeployments
                .AsNoTracking()
                .Where(d => d.MapDifficulty == mapDifficulty &&
                    (d.Status == "Deployed" || d.Status == "Pending") &&
                    d.Npc != null && d.Npc.Status == "active" &&
                    !_db.NpcChallenges.Any(challenge =>
                        challenge.DeploymentId == d.DeploymentId &&
                        challenge.State == "Completed"));

            var deployments = await deploymentQuery.OrderBy(d => d.Status == "Deployed" ? 0 : 1)
                .ThenBy(d => d.DeployedAt)
                .ThenBy(d => d.DeploymentId)
                .Select(d => new NpcDeploymentResponse
                {
                    DeploymentId = d.DeploymentId,
                    NpcId = d.NpcId,
                    NpcName = d.Npc!.NpcName,
                    NpcImage = d.Npc!.NpcImage ?? "",
                    AnimationScenePath = d.Npc!.AnimationScenePath,
                    MapDifficulty = d.MapDifficulty,
                    PositionX = d.PositionX,
                    PositionY = d.PositionY,
                    PositionMarker = d.PositionMarker,
                    Status = d.Status,
                    DeployedAt = d.DeployedAt
                })
                .ToListAsync(cancellationToken);

            return Ok(deployments);
        }

        [HttpPost("deployments/process-queue")]
        [Authorize(Roles = "student,admin")]
        public async Task<IActionResult> ProcessDeploymentQueue(
            NpcDeploymentQueueRequest request,
            CancellationToken cancellationToken)
        {
            if (!IsDifficulty(request.MapDifficulty))
                return BadRequest("Map difficulty must be Easy, Medium, or Hard.");
            if (request.AvailableSpawnPoints == null)
                return BadRequest("Available spawn points are required.");
            if (request.AvailableSpawnPoints.Any(point =>
                string.IsNullOrWhiteSpace(point.PositionMarker) ||
                !float.IsFinite(point.PositionX) ||
                !float.IsFinite(point.PositionY)))
                return BadRequest("Every available spawn point must have a marker and finite coordinates.");

            string difficulty = NormalizeDifficulty(request.MapDifficulty);
            var distinctMarkers = new HashSet<string>(StringComparer.Ordinal);
            foreach (NpcSpawnPointRequest point in request.AvailableSpawnPoints)
            {
                if (!distinctMarkers.Add(point.PositionMarker.Trim()))
                    return BadRequest("Available spawn point markers must be unique.");
            }

            if (request.AvailableSpawnPoints.Count == 0)
                return Ok(Array.Empty<NpcDeploymentResponse>());

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            await LockDeploymentQueueAsync(difficulty, cancellationToken);

            var completedDeployments = await _db.NpcDeployments
                .Where(deployment => deployment.MapDifficulty == difficulty &&
                    deployment.Status == "Deployed" &&
                    _db.NpcChallenges.Any(challenge =>
                        challenge.DeploymentId == deployment.DeploymentId &&
                        challenge.State == "Completed"))
                .ToListAsync(cancellationToken);
            foreach (NpcDeployment deployment in completedDeployments)
            {
                deployment.Status = "Cancelled";
                deployment.PositionX = null;
                deployment.PositionY = null;
                deployment.PositionMarker = null;
            }

            var occupiedMarkers = await _db.NpcDeployments
                .Where(deployment => deployment.MapDifficulty == difficulty &&
                    deployment.Status == "Deployed" &&
                    deployment.PositionMarker != null)
                .Select(deployment => deployment.PositionMarker!)
                .ToListAsync(cancellationToken);
            var availableSpawnPoints = new Queue<NpcSpawnPointRequest>(
                request.AvailableSpawnPoints
                    .Where(point => !occupiedMarkers.Contains(point.PositionMarker.Trim()))
                    .OrderBy(point => point.PositionMarker));
            var pendingDeployments = await _db.NpcDeployments
                .Include(deployment => deployment.Npc)
                .Where(deployment => deployment.MapDifficulty == difficulty &&
                    deployment.Status == "Pending" &&
                    deployment.Npc != null &&
                    deployment.Npc.Status == "active" &&
                    !_db.NpcChallenges.Any(challenge =>
                        challenge.DeploymentId == deployment.DeploymentId &&
                        challenge.State == "Completed"))
                .OrderBy(deployment => deployment.DeployedAt)
                .ThenBy(deployment => deployment.DeploymentId)
                .ToListAsync(cancellationToken);

            var newlyDeployed = new List<NpcDeploymentResponse>();
            foreach (NpcDeployment deployment in pendingDeployments)
            {
                if (availableSpawnPoints.Count == 0)
                    break;

                NpcSpawnPointRequest spawnPoint = availableSpawnPoints.Dequeue();
                deployment.PositionMarker = spawnPoint.PositionMarker.Trim();
                deployment.PositionX = spawnPoint.PositionX;
                deployment.PositionY = spawnPoint.PositionY;
                deployment.Status = "Deployed";
                newlyDeployed.Add(ToDeploymentResponse(deployment, deployment.Npc!));
            }

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Ok(newlyDeployed);
        }

        [HttpDelete("deployments/{deploymentId:int}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Undeploy(int deploymentId, CancellationToken cancellationToken)
        {
            var deployment = await _db.NpcDeployments
                .FirstOrDefaultAsync(d => d.DeploymentId == deploymentId &&
                    (d.Status == "Deployed" || d.Status == "Pending"), cancellationToken);
            if (deployment == null)
                return NotFound("Active or pending NPC deployment was not found.");

            deployment.Status = "Cancelled";
            deployment.PositionX = null;
            deployment.PositionY = null;
            deployment.PositionMarker = null;
            await _db.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        private async Task LockDeploymentQueueAsync(string difficulty, CancellationToken cancellationToken)
        {
            string lockKey = "npc-deployment-" + difficulty.ToLowerInvariant();
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtext({lockKey}))",
                cancellationToken);
        }

        private static NpcDeploymentResponse ToDeploymentResponse(
            NpcDeployment deployment,
            Npc npc)
        {
            return new NpcDeploymentResponse
            {
                DeploymentId = deployment.DeploymentId,
                NpcId = deployment.NpcId,
                NpcName = npc.NpcName,
                NpcImage = npc.NpcImage ?? "",
                AnimationScenePath = npc.AnimationScenePath,
                MapDifficulty = deployment.MapDifficulty,
                PositionX = deployment.PositionX,
                PositionY = deployment.PositionY,
                PositionMarker = deployment.PositionMarker,
                Status = deployment.Status,
                DeployedAt = deployment.DeployedAt
            };
        }

        private int GetUserId()
        {
            return int.TryParse(User.FindFirstValue("userId"), out int userId) ? userId : 0;
        }

        private static bool HasValidAnswerChoices(string choicesJson, string correctAnswer)
        {
            List<string>? choices;
            try
            {
                choices = JsonSerializer.Deserialize<List<string>>(choicesJson);
            }
            catch (JsonException)
            {
                return false;
            }

            if (choices == null)
                return false;

            List<string> normalized = NormalizeAnswerChoices(choices);
            return normalized.Count == 0 ||
                (normalized.Count >= 2 &&
                    normalized.Contains(correctAnswer.Trim(), StringComparer.OrdinalIgnoreCase));
        }

        private static List<string> NormalizeAnswerChoices(List<string> choices)
        {
            return choices
                .Where(choice => !string.IsNullOrWhiteSpace(choice))
                .Select(choice => choice.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool IsDifficulty(string? difficulty)
        {
            return string.Equals(difficulty?.Trim(), "Easy", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(difficulty?.Trim(), "Medium", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(difficulty?.Trim(), "Hard", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSupportedMiniGameType(string gameType)
        {
            return gameType == "MatchThePairs" ||
                gameType == "FallingDuck" ||
                gameType == "QuickTapChallenge";
        }

        private static string NormalizeDifficulty(string difficulty)
        {
            string value = difficulty.Trim().ToLowerInvariant();
            return char.ToUpperInvariant(value[0]) + value.Substring(1);
        }

        private static bool HasValidImagePath(string imagePath)
        {
            string extension = Path.GetExtension(imagePath).ToLowerInvariant();
            return imagePath.StartsWith("res://Items/NPC/", StringComparison.Ordinal) &&
                (extension == ".png" || extension == ".jpg" || extension == ".jpeg");
        }

        private static bool IsValidAnimationScenePath(string? scenePath, string difficulty)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
                return true;

            string folderName = NormalizeDifficulty(difficulty) + "MapNPC";
            string expectedPrefix = $"res://scene/AdventureGame/NPC/{folderName}/";
            return scenePath.StartsWith(expectedPrefix, StringComparison.Ordinal) &&
                scenePath.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase) &&
                !scenePath.Contains("..", StringComparison.Ordinal);
        }

        private static string? NormalizeAnimationScenePath(string? scenePath)
        {
            return string.IsNullOrWhiteSpace(scenePath) ? null : scenePath.Trim();
        }
    }

    public class NpcRequest
    {
        public string NpcName { get; set; } = "";
        public string? NpcImage { get; set; }
        public string? AnimationScenePath { get; set; }
        public string Difficulty { get; set; } = "Easy";
        public string? Status { get; set; }
    }

    public class NpcDeploymentRequest
    {
        public string MapDifficulty { get; set; } = "Easy";
        public float? PositionX { get; set; }
        public float? PositionY { get; set; }
        public string? PositionMarker { get; set; }
    }

    public class NpcDeploymentQueueRequest
    {
        public string MapDifficulty { get; set; } = "";
        public List<NpcSpawnPointRequest> AvailableSpawnPoints { get; set; } = new();
    }

    public class NpcSpawnPointRequest
    {
        public string PositionMarker { get; set; } = "";
        public float PositionX { get; set; }
        public float PositionY { get; set; }
    }

    public class NpcDeploymentResponse
    {
        public int DeploymentId { get; set; }
        public int NpcId { get; set; }
        public string NpcName { get; set; } = "";
        public string NpcImage { get; set; } = "";
        public string? AnimationScenePath { get; set; }
        public string MapDifficulty { get; set; } = "";
        public string Status { get; set; } = "";
        public string? PositionMarker { get; set; }
        public float? PositionX { get; set; }
        public float? PositionY { get; set; }
        public DateTime DeployedAt { get; set; }
    }

    public class NpcConfigurationRequest
    {
        public string SubjectName { get; set; } = "";
        public int? MiniGameId { get; set; }
        public string Question { get; set; } = "";
        public List<string>? AnswerChoices { get; set; }
        public string CorrectAnswer { get; set; } = "";
        public string? AnswerTip { get; set; }
        public int TimerSeconds { get; set; } = 30;
    }

    public class NpcConfigurationResponse
    {
        public int NpcId { get; set; }
        public int? DeploymentId { get; set; }
        public string SubjectName { get; set; } = "";
        public int? MiniGameId { get; set; }
        public string MiniGameName { get; set; } = "";
        public string Question { get; set; } = "";
        public List<string> AnswerChoices { get; set; } = new();
        public string CorrectAnswer { get; set; } = "";
        public string AnswerTip { get; set; } = "";
        public int TimerSeconds { get; set; } = 30;
    }

    public class StudentNpcConfigurationResponse
    {
        public int NpcId { get; set; }
        public string NpcName { get; set; } = "";
        public int DeploymentId { get; set; }
        public string Difficulty { get; set; } = "";
        public string? PositionMarker { get; set; }
        public int QuestionId { get; set; }
        public string SubjectName { get; set; } = "";
        public int? MiniGameId { get; set; }
        public string MiniGameName { get; set; } = "";
        public int TimerSeconds { get; set; }
        public bool AlreadyStarted { get; set; }
        public string ChallengeState { get; set; } = "";
        public string AnswerTip { get; set; } = "";
    }

    public class StudentChallengeStartResponse
    {
        public int ChallengeId { get; set; }
        public Guid AttemptId { get; set; }
        public int NpcId { get; set; }
        public int DeploymentId { get; set; }
        public int QuestionId { get; set; }
        public string GameType { get; set; } = "";
        public int TimerSeconds { get; set; }
        public string Difficulty { get; set; } = "";
    }

    public class CharacterChallengeStartRequest
    {
        public string CharacterName { get; set; } = "Character";
    }

    public class CharacterChallengeStartResponse
    {
        public int ChallengeId { get; set; }
        public Guid AttemptId { get; set; }
        public int NpcId { get; set; }
        public string NpcName { get; set; } = "";
        public int DeploymentId { get; set; }
        public string CharacterName { get; set; } = "";
        public string GameType { get; set; } = "HigherLower";
        public string State { get; set; } = "";
    }

    public class StudentChallengeQuestionResponse
    {
        public int QuestionId { get; set; }
        public int? NpcId { get; set; }
        public int DeploymentId { get; set; }
        public string SubjectName { get; set; } = "";
        public string Question { get; set; } = "";
        public List<string> AnswerChoices { get; set; } = new();
        public int TimerSeconds { get; set; }
        public DateTime QuestionStartedAt { get; set; }
        public int RemainingSeconds { get; set; }
        public string Difficulty { get; set; } = "";
    }
}
