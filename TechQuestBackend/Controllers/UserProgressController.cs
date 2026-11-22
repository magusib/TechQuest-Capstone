using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechQuestBackend.Data;
using TechQuestBackend.Models;

namespace TechQuestBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "student")]
public sealed class UserProgressController : ControllerBase
{
    private readonly TechQuestDbContext _db;

    public UserProgressController(TechQuestDbContext db) => _db = db;

    /// <summary>Returns completed Adventure Game question history for the authenticated student.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserProgressResponse>>> Get(
        [FromQuery] string difficulty,
        CancellationToken cancellationToken)
    {
        if (!IsSupportedDifficulty(difficulty))
            return BadRequest("Difficulty must be Easy, Medium, or Hard.");

        string? userIdClaim = User.FindFirstValue("userId");
        if (!int.TryParse(userIdClaim, out int userId))
            return Unauthorized();

        StudentProfile? profile = await _db.StudentProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (profile == null)
            return Forbid();

        var history = await _db.NpcChallenges
            .AsNoTracking()
            .Where(item => item.StudentId == profile.StudentId &&
                item.State == "Completed" &&
                item.Difficulty.ToLower() == difficulty.Trim().ToLower())
            .OrderByDescending(item => item.CompletedAt)
            .ThenByDescending(item => item.ChallengeId)
            .Select(item => new UserProgressResponse(
                item.StudentId,
                item.AttemptId,
                item.Difficulty,
                item.SubjectName,
                item.QuestionId,
                item.QuestionText,
                item.CorrectAnswer,
                item.SelectedAnswer,
                item.QuestionTimedOut ? "TIME OUT" : item.QuestionCorrect == true ? "CORRECT" : "INCORRECT",
                item.CoinsChanged,
                item.PointsChanged,
                item.State,
                item.CompletedAt))
            .ToListAsync(cancellationToken);

        return Ok(history);
    }

    private static bool IsSupportedDifficulty(string? difficulty) =>
        string.Equals(difficulty, "Easy", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(difficulty, "Medium", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(difficulty, "Hard", StringComparison.OrdinalIgnoreCase);
}

/// <summary>Immutable completed Adventure Game question history returned to a student.</summary>
public sealed record UserProgressResponse(
    int StudentId,
    Guid AdventureQuizSessionId,
    string Difficulty,
    string Subject,
    int QuestionId,
    string Question,
    string CorrectAnswer,
    string? StudentAnswer,
    string Status,
    int CoinsEarned,
    int PointsEarned,
    string QuizCompletionStatus,
    DateTime? CompletionDate);
