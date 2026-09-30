using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
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
        private readonly IWebHostEnvironment _environment;

        public AccountController(TechQuestDbContext db, IWebHostEnvironment environment)
        {
            _db = db;
            _environment = environment;
        }

        [HttpPost("avatar")]
        [Authorize(Roles = "admin,professor")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> UploadAvatar([FromForm] IFormFile image)
        {
            if (image == null || image.Length == 0)
                return BadRequest("Select an image to upload.");

            string extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (extension is not ".png" and not ".jpg" and not ".jpeg")
                return BadRequest("Avatar must be a PNG or JPEG image.");

            if (!int.TryParse(User.FindFirstValue("userId"), out int userId))
                return Unauthorized();

            var user = await _db.Users.FindAsync(userId);
            if (user == null)
                return NotFound();

            string uploadFolder = Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), "uploads", "avatars");
            Directory.CreateDirectory(uploadFolder);

            string fileName = $"{userId}_{Guid.NewGuid():N}{extension}";
            string filePath = Path.Combine(uploadFolder, fileName);
            await using (var stream = System.IO.File.Create(filePath))
                await image.CopyToAsync(stream);

            user.Avatar = $"/uploads/avatars/{fileName}";
            await _db.SaveChangesAsync();
            return Ok(new { avatar = user.Avatar });
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

            if (user.Role == "student")
            {
                var profile = await _db.StudentProfiles
                    .AsNoTracking()
                    .SingleOrDefaultAsync(item => item.UserId == user.Id);

                totalCoins = profile?.TotalCoins ?? 0;
                totalPoints = profile?.TotalPoints ?? 0;
            }

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

        [HttpDelete("users/{userId:int}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User was not found.");

            if (!string.Equals(user.Role, "student", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(user.Role, "professor", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Only student and professor accounts can be deleted here.");

            if (string.Equals(user.Role, "student", StringComparison.OrdinalIgnoreCase))
            {
                var profile = await _db.StudentProfiles
                    .SingleOrDefaultAsync(item => item.UserId == user.Id);
                if (profile != null)
                    _db.StudentProfiles.Remove(profile);
            }

            if (string.Equals(user.Role, "professor", StringComparison.OrdinalIgnoreCase))
            {
                var uploads = await _db.ProfessorUploads
                    .Where(item => item.ProfessorId == user.Id)
                    .ToListAsync();
                _db.ProfessorUploads.RemoveRange(uploads);
            }

            var emailVerifications = await _db.EmailVerifications
                .Where(item => item.Email == user.Email)
                .ToListAsync();
            _db.EmailVerifications.RemoveRange(emailVerifications);

            var passwordResets = await _db.PasswordResets
                .Where(item => item.Email == user.Email)
                .ToListAsync();
            _db.PasswordResets.RemoveRange(passwordResets);

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
