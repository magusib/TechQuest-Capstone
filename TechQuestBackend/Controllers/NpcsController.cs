using System.Security.Claims;
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
                .OrderBy(q => q.QuestionId)
                .Select(q => new NpcConfigurationResponse
                {
                    NpcId = npcId,
                    SubjectName = q.SubjectName,
                    MiniGameId = q.MiniGameId,
                    MiniGameName = q.MiniGame == null ? "" : q.MiniGame.GameName,
                    Question = q.QuestionText,
                    CorrectAnswer = q.CorrectAnswer,
                    AnswerTip = q.AnswerTip
                })
                .FirstOrDefaultAsync(cancellationToken);

            return Ok(question ?? new NpcConfigurationResponse { NpcId = npcId });
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

            if (request.MiniGameId.HasValue &&
                !await _db.MiniGames.AnyAsync(m => m.MiniGameId == request.MiniGameId.Value, cancellationToken))
                return BadRequest("Mini-game was not found.");

            var question = await _db.Questions
                .Where(q => q.NpcId == npcId)
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
            question.CorrectAnswer = request.CorrectAnswer.Trim();
            question.AnswerTip = request.AnswerTip?.Trim() ?? "";

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
                CorrectAnswer = question.CorrectAnswer,
                AnswerTip = question.AnswerTip
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
            if (string.IsNullOrWhiteSpace(request.NpcImage))
                return BadRequest("NPC image is required.");
            if (!HasValidImagePath(request.NpcImage))
                return BadRequest("NPC image must be an uploaded PNG, JPG, or JPEG image.");
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
                    return BadRequest("NPC image must be an uploaded PNG, JPG, or JPEG image.");
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
                .Where(d => d.NpcId == npcId && d.Status == "deployed")
                .ToListAsync(cancellationToken);
            foreach (var deployment in deployments)
                deployment.Status = "removed";

            await _db.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        [HttpPost("image")]
        [Authorize(Roles = "admin")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> UploadImage([FromForm] IFormFile image, CancellationToken cancellationToken)
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
            if (string.IsNullOrWhiteSpace(request.PositionMarker))
                return BadRequest("A map spawn marker is required.");

            string difficulty = NormalizeDifficulty(request.MapDifficulty);
            var npc = await _db.Npcs.FirstOrDefaultAsync(
                n => n.NpcId == npcId && n.Status == "active", cancellationToken);
            if (npc == null)
                return NotFound("NPC was not found or is inactive.");
            if (!string.Equals(npc.Difficulty, difficulty, StringComparison.OrdinalIgnoreCase))
                return BadRequest("NPC difficulty must match the map difficulty.");

            var deployment = new NpcDeployment
            {
                NpcId = npcId,
                MapDifficulty = difficulty,
                PositionX = request.PositionX,
                PositionY = request.PositionY,
                PositionMarker = request.PositionMarker.Trim(),
                DeployedBy = GetUserId(),
                DeployedAt = DateTime.UtcNow
            };
            _db.NpcDeployments.Add(deployment);

            await _db.SaveChangesAsync(cancellationToken);
            return Ok(deployment);
        }

        [HttpGet("deployments")]
        [Authorize(Roles = "student,admin")]
        public async Task<IActionResult> GetDeployments([FromQuery] string difficulty, CancellationToken cancellationToken)
        {
            if (!IsDifficulty(difficulty))
                return BadRequest("Difficulty must be Easy, Medium, or Hard.");

            string mapDifficulty = NormalizeDifficulty(difficulty);
            var deployments = await _db.NpcDeployments
                .AsNoTracking()
                .Where(d => d.MapDifficulty == mapDifficulty && d.Status == "deployed" && d.Npc != null && d.Npc.Status == "active")
                .Select(d => new
                {
                    d.DeploymentId,
                    d.NpcId,
                    NpcName = d.Npc!.NpcName,
                    NpcImage = d.Npc!.NpcImage,
                    AnimationScenePath = d.Npc!.AnimationScenePath,
                    d.MapDifficulty,
                    d.PositionX,
                    d.PositionY,
                    d.PositionMarker
                })
                .ToListAsync(cancellationToken);

            return Ok(deployments);
        }

        [HttpDelete("deployments/{deploymentId:int}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Undeploy(int deploymentId, CancellationToken cancellationToken)
        {
            var deployment = await _db.NpcDeployments
                .FirstOrDefaultAsync(d => d.DeploymentId == deploymentId && d.Status == "deployed", cancellationToken);
            if (deployment == null)
                return NotFound("Active NPC deployment was not found.");

            deployment.Status = "removed";
            await _db.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        private int GetUserId()
        {
            return int.TryParse(User.FindFirstValue("userId"), out int userId) ? userId : 0;
        }

        private static bool IsDifficulty(string? difficulty)
        {
            return string.Equals(difficulty?.Trim(), "Easy", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(difficulty?.Trim(), "Medium", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(difficulty?.Trim(), "Hard", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeDifficulty(string difficulty)
        {
            string value = difficulty.Trim().ToLowerInvariant();
            return char.ToUpperInvariant(value[0]) + value.Substring(1);
        }

        private static bool HasValidImagePath(string imagePath)
        {
            string extension = Path.GetExtension(imagePath).ToLowerInvariant();
            return imagePath.StartsWith("/uploads/npcs/", StringComparison.Ordinal) &&
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
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public string PositionMarker { get; set; } = "";
    }

    public class NpcConfigurationRequest
    {
        public string SubjectName { get; set; } = "";
        public int? MiniGameId { get; set; }
        public string Question { get; set; } = "";
        public string CorrectAnswer { get; set; } = "";
        public string? AnswerTip { get; set; }
    }

    public class NpcConfigurationResponse
    {
        public int NpcId { get; set; }
        public string SubjectName { get; set; } = "";
        public int? MiniGameId { get; set; }
        public string MiniGameName { get; set; } = "";
        public string Question { get; set; } = "";
        public string CorrectAnswer { get; set; } = "";
        public string AnswerTip { get; set; } = "";
    }
}
