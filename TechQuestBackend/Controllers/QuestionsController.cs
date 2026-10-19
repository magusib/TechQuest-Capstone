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
    public class QuestionsController : ControllerBase
    {
        private readonly TechQuestDbContext _db;

        public QuestionsController(TechQuestDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        [Authorize(Roles = "student,professor,admin")]
        public async Task<IActionResult> Get([FromQuery] int? npcId, [FromQuery] int? miniGameId)
        {
            var query = _db.Questions.AsNoTracking();

            if (npcId.HasValue)
                query = query.Where(q => q.NpcId == npcId);

            if (miniGameId.HasValue)
                query = query.Where(q => q.MiniGameId == miniGameId);

            if (User.IsInRole("student"))
            {
                return Ok(await query.OrderBy(q => q.QuestionId)
                    .Select(q => new StudentQuestionResponse
                    {
                        QuestionId = q.QuestionId,
                        NpcId = q.NpcId,
                        MiniGameId = q.MiniGameId,
                        SubjectName = q.SubjectName,
                        QuestionText = q.QuestionText
                    })
                    .ToListAsync());
            }

            return Ok(await query.OrderBy(q => q.QuestionId).ToListAsync());
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create(QuestionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SubjectName) ||
                string.IsNullOrWhiteSpace(request.Question) ||
                string.IsNullOrWhiteSpace(request.CorrectAnswer))
            {
                return BadRequest("Subject, question, and correct answer are required.");
            }

            if (!request.NpcId.HasValue && !request.MiniGameId.HasValue)
                return BadRequest("Question must be linked to an NPC, a mini-game, or both.");

            if (request.NpcId.HasValue && !await _db.Npcs.AnyAsync(n => n.NpcId == request.NpcId.Value))
                return BadRequest("NPC was not found.");

            if (request.MiniGameId.HasValue && !await _db.MiniGames.AnyAsync(m => m.MiniGameId == request.MiniGameId.Value))
                return BadRequest("Mini-game was not found.");

            var question = new Question
            {
                NpcId = request.NpcId,
                MiniGameId = request.MiniGameId,
                SubjectName = request.SubjectName.Trim(),
                QuestionText = request.Question.Trim(),
                CorrectAnswer = request.CorrectAnswer.Trim(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = GetUserId()
            };

            _db.Questions.Add(question);
            await _db.SaveChangesAsync();
            return Ok(question);
        }

        private int GetUserId()
        {
            return int.TryParse(User.FindFirstValue("userId"), out int userId) ? userId : 0;
        }
    }

    public class QuestionRequest
    {
        public int? NpcId { get; set; }
        public int? MiniGameId { get; set; }
        public string SubjectName { get; set; } = "";
        public string Question { get; set; } = "";
        public string CorrectAnswer { get; set; } = "";
    }

    public class StudentQuestionResponse
    {
        public int QuestionId { get; set; }
        public int? NpcId { get; set; }
        public int? MiniGameId { get; set; }
        public string SubjectName { get; set; } = "";
        public string QuestionText { get; set; } = "";
    }
}
