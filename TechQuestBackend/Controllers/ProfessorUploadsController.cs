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
    public class ProfessorUploadsController : ControllerBase
    {
        private readonly TechQuestDbContext _db;

        public ProfessorUploadsController(TechQuestDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        [Authorize(Roles = "professor,admin")]
        public async Task<IActionResult> Get()
        {
            return Ok(await _db.ProfessorUploads.AsNoTracking().OrderByDescending(u => u.UploadedAt).ToListAsync());
        }

        [HttpPost("metadata")]
        [Authorize(Roles = "professor")]
        public async Task<IActionResult> SaveMetadata(ProfessorUploadRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FileName))
                return BadRequest("File name is required.");

            string extension = Path.GetExtension(request.FileName).ToLowerInvariant();
            if (extension != ".csv" && extension != ".xlsx")
                return BadRequest("Only CSV or XLSX quiz files are supported.");

            int userId = GetUserId();
            var professor = await _db.Users.SingleOrDefaultAsync(u => u.Id == userId && u.Role == "professor");
            if (professor == null)
                return Unauthorized();

            var upload = new ProfessorUpload
            {
                ProfessorId = professor.Id,
                FileName = request.FileName.Trim(),
                UploadedAt = DateTime.UtcNow,
                CreatedBy = professor.Id
            };

            _db.ProfessorUploads.Add(upload);
            await _db.SaveChangesAsync();
            return Ok(upload);
        }

        [HttpGet("template")]
        [Authorize(Roles = "professor")]
        public IActionResult Template()
        {
            return Ok(new
            {
                columns = new[]
                {
                    "SubjectName",
                    "Question",
                    "CorrectAnswer",
                    "NpcId",
                    "MiniGameId"
                }
            });
        }

        private int GetUserId()
        {
            return int.TryParse(User.FindFirstValue("userId"), out int userId) ? userId : 0;
        }
    }

    public class ProfessorUploadRequest
    {
        public string FileName { get; set; } = "";
    }
}
