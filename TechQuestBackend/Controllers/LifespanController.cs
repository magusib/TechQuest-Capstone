using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechQuestBackend.Data;

namespace TechQuestBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "student")]
    public class LifespanController : ControllerBase
    {
        private const int MaximumLifespan = 100;
        private const int PurchaseCost = 500;
        private static readonly TimeSpan AutomaticRecoveryDelay = TimeSpan.FromHours(3);
        private static readonly TimeSpan WalkingRecoveryDelay = TimeSpan.FromMinutes(1);
        private readonly TechQuestDbContext _db;

        public LifespanController(TechQuestDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            int userId = GetUserId();
            if (userId <= 0)
                return Unauthorized();

            await using var transaction = await _db.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted, cancellationToken);
            var profile = await GetLockedProfileAsync(userId, cancellationToken);
            if (profile == null)
                return BadRequest("Student profile was not found.");

            ApplyAutomaticRecovery(profile);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Ok(CreateResponse(profile));
        }

        [HttpPost("deduct")]
        public async Task<IActionResult> Deduct(DeductLifespanRequest request, CancellationToken cancellationToken)
        {
            if (request.Amount <= 0 || request.Amount > MaximumLifespan)
                return BadRequest("The lifespan deduction amount is invalid.");

            if (string.IsNullOrWhiteSpace(request.Reason))
                return BadRequest("A lifespan deduction reason is required.");

            int userId = GetUserId();
            if (userId <= 0)
                return Unauthorized();

            await using var transaction = await _db.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted, cancellationToken);
            var profile = await GetLockedProfileAsync(userId, cancellationToken);
            if (profile == null)
                return BadRequest("Student profile was not found.");

            ApplyAutomaticRecovery(profile);
            profile.Lifespan = Math.Max(0, profile.Lifespan - request.Amount);
            profile.LifespanAutoRecoveryAt = profile.Lifespan == 0
                ? DateTime.UtcNow.Add(AutomaticRecoveryDelay)
                : null;
            profile.LifespanWalkRecoveryAt = null;

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Ok(CreateResponse(profile));
        }

        [HttpPost("walking-recovery")]
        public async Task<IActionResult> WalkingRecovery(CancellationToken cancellationToken)
        {
            int userId = GetUserId();
            if (userId <= 0)
                return Unauthorized();

            await using var transaction = await _db.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted, cancellationToken);
            var profile = await GetLockedProfileAsync(userId, cancellationToken);
            if (profile == null)
                return BadRequest("Student profile was not found.");

            ApplyAutomaticRecovery(profile);
            DateTime now = DateTime.UtcNow;
            if (profile.Lifespan >= MaximumLifespan)
            {
                await transaction.CommitAsync(cancellationToken);
                return Ok(CreateResponse(profile));
            }

            if (profile.LifespanWalkRecoveryAt.HasValue &&
                now < profile.LifespanWalkRecoveryAt.Value.Add(WalkingRecoveryDelay))
            {
                await transaction.CommitAsync(cancellationToken);
                return Ok(CreateResponse(profile));
            }

            profile.Lifespan++;
            profile.LifespanWalkRecoveryAt = now;
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Ok(CreateResponse(profile));
        }

        [HttpPost("purchase")]
        public async Task<IActionResult> Purchase(CancellationToken cancellationToken)
        {
            int userId = GetUserId();
            if (userId <= 0)
                return Unauthorized();

            await using var transaction = await _db.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted, cancellationToken);
            var profile = await GetLockedProfileAsync(userId, cancellationToken);
            if (profile == null)
                return BadRequest("Student profile was not found.");

            ApplyAutomaticRecovery(profile);
            if (profile.Lifespan >= MaximumLifespan)
                return Conflict("Lifespan is already full.");

            if (profile.TotalCoins < PurchaseCost)
                return BadRequest("Not enough Coins.");

            profile.TotalCoins -= PurchaseCost;
            profile.Lifespan = MaximumLifespan;
            profile.LifespanAutoRecoveryAt = null;
            profile.LifespanWalkRecoveryAt = null;

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Ok(new LifespanResponse
            {
                Lifespan = profile.Lifespan,
                MaximumLifespan = MaximumLifespan,
                TotalCoins = profile.TotalCoins,
                LifespanAutoRecoveryAtUtc = ToUtcOffset(profile.LifespanAutoRecoveryAt),
                LifespanWalkingRecoveryAvailableAtUtc = ToUtcOffset(profile.LifespanWalkRecoveryAt?.Add(WalkingRecoveryDelay)),
                Message = "Lifespan restored successfully."
            });
        }

        private async Task<TechQuestBackend.Models.StudentProfile?> GetLockedProfileAsync(
            int userId, CancellationToken cancellationToken)
        {
            return await _db.StudentProfiles
                .FromSqlInterpolated($"SELECT * FROM \"Student_profile\" WHERE \"UserId\" = {userId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
        }

        private static void ApplyAutomaticRecovery(TechQuestBackend.Models.StudentProfile profile)
        {
            if (profile.Lifespan > 0)
            {
                // Full automatic recovery is only available when the player is
                // completely depleted, not when lifespan is merely below 100.
                profile.LifespanAutoRecoveryAt = null;
                return;
            }

            DateTime now = DateTime.UtcNow;
            if (!profile.LifespanAutoRecoveryAt.HasValue)
            {
                // Backfill a schedule for profiles created before the recovery
                // timestamp was stored, so the client can always show the timer.
                profile.LifespanAutoRecoveryAt = now.Add(AutomaticRecoveryDelay);
                return;
            }

            if (now >= profile.LifespanAutoRecoveryAt.Value)
            {
                profile.Lifespan = MaximumLifespan;
                profile.LifespanAutoRecoveryAt = null;
                profile.LifespanWalkRecoveryAt = null;
            }
        }

        private static LifespanResponse CreateResponse(TechQuestBackend.Models.StudentProfile profile)
        {
            return new LifespanResponse
            {
                Lifespan = profile.Lifespan,
                MaximumLifespan = MaximumLifespan,
                TotalCoins = profile.TotalCoins,
                LifespanAutoRecoveryAtUtc = ToUtcOffset(profile.LifespanAutoRecoveryAt),
                LifespanWalkingRecoveryAvailableAtUtc = ToUtcOffset(profile.LifespanWalkRecoveryAt?.Add(WalkingRecoveryDelay))
            };
        }

        private static DateTimeOffset? ToUtcOffset(DateTime? value)
        {
            return value.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
                : null;
        }

        private int GetUserId()
        {
            return int.TryParse(User.FindFirstValue("userId"), out int userId) ? userId : 0;
        }
    }

    public class DeductLifespanRequest
    {
        public int Amount { get; set; }
        public string Reason { get; set; } = "";
    }

    public class LifespanResponse
    {
        public int Lifespan { get; set; }
        public int MaximumLifespan { get; set; }
        public int TotalCoins { get; set; }
        public DateTimeOffset? LifespanAutoRecoveryAtUtc { get; set; }
        public DateTimeOffset? LifespanWalkingRecoveryAvailableAtUtc { get; set; }
        public string Message { get; set; } = "";
    }
}
