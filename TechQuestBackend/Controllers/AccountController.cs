using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechQuestBackend.Data;

namespace TechQuestBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AccountController : ControllerBase
    {
        private readonly TechQuestDbContext _db;

        public AccountController(TechQuestDbContext db)
        {
            _db = db;
        }

        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            if (!int.TryParse(User.FindFirstValue("userId"), out int userId))
            {
                return Unauthorized();
            }

            var user = await _db.Users
                .AsNoTracking()
                .Where(item => item.Id == userId)
                .Select(item => new
                {
                    item.Id,
                    item.FirstName,
                    item.LastName,
                    item.YearLevel,
                    item.Email,
                    item.Role,
                    item.Avatar,
                    item.CreatedAt,
                    item.IsEmailVerified
                })
                .SingleOrDefaultAsync();

            if (user == null)
                return NotFound();

            int totalCoins = 0;
            int totalPoints = 0;

            return Ok(new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.YearLevel,
                user.Email,
                user.Role,
                user.Avatar,
                user.CreatedAt,
                user.IsEmailVerified,
                totalCoins,
                totalPoints
            });
        }

        [HttpGet("users")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Users()
        {
            var users = await _db.Users
                .AsNoTracking()
                .Select(item => new
                {
                    item.Id,
                    item.FirstName,
                    item.LastName,
                    item.YearLevel,
                    item.Email,
                    item.Role,
                    item.Avatar,
                    item.CreatedAt,
                    item.IsEmailVerified
                })
                .ToListAsync();

            return Ok(users);
        }
    }
}
