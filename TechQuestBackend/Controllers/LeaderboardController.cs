using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechQuestBackend.Data;
using TechQuestBackend.Models;

namespace TechQuestBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "student,professor,admin")]
    public class LeaderboardController : ControllerBase
    {
        private readonly TechQuestDbContext _db;

        public LeaderboardController(TechQuestDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var students = await _db.StudentProfiles
                .AsNoTracking()
                .Include(p => p.User)
                .OrderByDescending(p => p.TotalPoints)
                .ThenBy(p => p.StudentId)
                .Select(p => new
                {
                    p.StudentId,
                    UserId = p.UserId,
                    FirstName = p.User == null ? "" : p.User.FirstName,
                    LastName = p.User == null ? "" : p.User.LastName,
                    YearLevel = p.User == null ? null : p.User.YearLevel,
                    Avatar = p.User == null ? null : p.User.Avatar,
                    p.TotalPoints,
                    p.TotalCoins
                })
                .ToListAsync();

            var ranked = students
                .Select((student, index) => new
                {
                    student.StudentId,
                    student.UserId,
                    student.FirstName,
                    student.LastName,
                    student.YearLevel,
                    student.Avatar,
                    student.TotalPoints,
                    student.TotalCoins,
                    RankPosition = index + 1
                });

            return Ok(ranked);
        }

        [HttpGet("previous")]
        public async Task<IActionResult> GetPrevious()
        {
            var history = await _db.LeaderboardHistories
                .AsNoTracking()
                .OrderByDescending(h => h.LeaderboardHistoryId)
                .Select(h => new
                {
                    h.ResetAt,
                    Entries = h.Entries
                        .OrderBy(e => e.RankPosition)
                        .Select(e => new
                        {
                            e.StudentId,
                            e.FirstName,
                            e.LastName,
                            e.YearLevel,
                            e.Avatar,
                            e.TotalPoints,
                            e.RankPosition
                        })
                })
                .FirstOrDefaultAsync();

            return Ok(history);
        }

        [HttpPost("reset")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Reset()
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            var topStudents = await _db.StudentProfiles
                .Include(p => p.User)
                .Where(p => p.User != null)
                .OrderByDescending(p => p.TotalPoints)
                .ThenBy(p => p.StudentId)
                .Take(3)
                .ToListAsync();

            var history = new LeaderboardHistory
            {
                ResetAt = DateTime.UtcNow,
                Entries = topStudents.Select((student, index) => new LeaderboardHistoryEntry
                {
                    StudentId = student.StudentId,
                    FirstName = student.User!.FirstName,
                    LastName = student.User.LastName,
                    YearLevel = student.User.YearLevel,
                    Avatar = student.User.Avatar,
                    TotalPoints = student.TotalPoints,
                    RankPosition = index + 1
                }).ToList()
            };

            _db.LeaderboardHistories.Add(history);

            var allStudents = await _db.StudentProfiles.ToListAsync();
            foreach (var student in allStudents)
                student.TotalPoints = 0;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { message = "Leaderboard reset successfully.", history.ResetAt });
        }
    }
}
