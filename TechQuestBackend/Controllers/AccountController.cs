using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechQuestBackend.Data;
using System.Net.Http.Headers;

namespace TechQuestBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AccountController : ControllerBase
    {
        private readonly TechQuestDbContext _db;
        private readonly IWebHostEnvironment _environment;
        private readonly IHttpClientFactory _httpClientFactory;

        public AccountController(
            TechQuestDbContext db,
            IWebHostEnvironment environment,
            IHttpClientFactory httpClientFactory)
        {
            _db = db;
            _environment = environment;
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("avatar")]
        [Authorize(Roles = "admin,professor")]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> UploadAvatar(IFormFile image, CancellationToken cancellationToken)
        {
            if (image == null || image.Length == 0)
                return BadRequest("Select an image to upload.");

            const long maxImageSize = 5 * 1024 * 1024;
            if (image.Length > maxImageSize)
                return BadRequest("Avatar must not be larger than 5 MB.");

            string extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (extension is not ".png" and not ".jpg" and not ".jpeg")
                return BadRequest("Avatar must be a PNG or JPEG image.");

            var user = await FindAuthenticatedUserAsync();
            if (user == null)
                return NotFound("The authenticated account was not found. Please log in again.");

            int userId = user.Id;
            string? supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL")?.TrimEnd('/');
            string? serviceRoleKey = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY");
            if (string.IsNullOrWhiteSpace(supabaseUrl) || string.IsNullOrWhiteSpace(serviceRoleKey))
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    "Avatar storage is not configured on the server.");

            string fileName = $"{user.Role}/{userId}/{Guid.NewGuid():N}{extension}";
            string uploadUrl = $"{supabaseUrl}/storage/v1/object/avatars/" +
                string.Join("/", fileName.Split('/').Select(Uri.EscapeDataString));
            string publicUrl = $"{supabaseUrl}/storage/v1/object/public/avatars/" +
                string.Join("/", fileName.Split('/').Select(Uri.EscapeDataString));

            using HttpRequestMessage uploadRequest = new(HttpMethod.Post, uploadUrl);
            uploadRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", serviceRoleKey);
            uploadRequest.Headers.Add("apikey", serviceRoleKey);
            uploadRequest.Headers.Add("x-upsert", "true");

            await using Stream imageStream = image.OpenReadStream();
            using StreamContent imageContent = new(imageStream);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue(
                extension == ".png" ? "image/png" : "image/jpeg");
            uploadRequest.Content = imageContent;

            HttpClient client = _httpClientFactory.CreateClient();
            using HttpResponseMessage uploadResponse = await client.SendAsync(
                uploadRequest, cancellationToken);
            if (!uploadResponse.IsSuccessStatusCode)
            {
                string storageError = await uploadResponse.Content.ReadAsStringAsync(cancellationToken);
                return StatusCode(StatusCodes.Status502BadGateway,
                    string.IsNullOrWhiteSpace(storageError)
                        ? "Avatar storage upload failed."
                        : "Avatar storage upload failed: " + storageError);
            }

            user.Avatar = publicUrl;
            await _db.SaveChangesAsync(cancellationToken);
            return Ok(new { avatar = user.Avatar });
        }

        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var user = await FindAuthenticatedUserAsync();

            if (user == null)
                return NotFound("The authenticated account was not found. Please log in again.");

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

        private async Task<Models.User?> FindAuthenticatedUserAsync()
        {
            if (int.TryParse(User.FindFirstValue("userId"), out int userId))
            {
                var userById = await _db.Users.SingleOrDefaultAsync(item => item.Id == userId);
                if (userById != null)
                    return userById;
            }

            string? email = User.FindFirstValue("email")
                ?? User.FindFirstValue(ClaimTypes.Email)
                ?? User.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(email))
                return null;

            return await _db.Users.SingleOrDefaultAsync(item =>
                item.Email.ToLower() == email.Trim().ToLower());
        }
    }
}
