using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechQuestBackend.Data;

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
                    student.TotalPoints,
                    student.TotalCoins,
                    RankPosition = index + 1
                });

            return Ok(ranked);
        }
    }
}
