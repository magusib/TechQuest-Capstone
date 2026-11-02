using System.Security.Claims;
using System.Text.Json;
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
    public class MultiplayerRoomsController : ControllerBase
    {
        private readonly TechQuestDbContext _db;

        public MultiplayerRoomsController(TechQuestDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        [Authorize(Roles = "professor,admin")]
        public async Task<IActionResult> Get()
        {
            int userId = GetUserId();
            string role = User.FindFirstValue("role") ?? "";

            IQueryable<MultiplayerRoom> query = _db.MultiplayerRooms.AsNoTracking();
            if (!string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
                query = query.Where(r => r.CreatedBy == userId);

            var roomEntities = await query
                .Include(r => r.Participants)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();

            return Ok(roomEntities.Select(ToRoomSummary));
        }

        [HttpGet("{roomId:int}")]
        [Authorize(Roles = "professor,admin,student")]
        public async Task<IActionResult> GetById(int roomId)
        {
            var room = await _db.MultiplayerRooms
                .Include(r => r.Participants)
                .AsNoTracking()
                .SingleOrDefaultAsync(r => r.RoomId == roomId);

            if (room == null)
                return NotFound("Room was not found.");

            if (!CanAccessRoom(room))
                return Forbid();

            return Ok(ToRoomSummary(room));
        }

        [HttpGet("code/{roomCode}")]
        [Authorize(Roles = "professor,admin,student")]
        public async Task<IActionResult> GetByCode(string roomCode)
        {
            string normalizedCode = NormalizeRoomCode(roomCode);
            var room = await _db.MultiplayerRooms
                .Include(r => r.Participants)
                .AsNoTracking()
                .SingleOrDefaultAsync(r => r.RoomCode.ToLower() == normalizedCode);

            if (room == null)
                return NotFound("Invalid room code");

            if (!CanAccessRoom(room))
                return Forbid();

            return Ok(ToRoomSummary(room));
        }

        [HttpPost("generate-code")]
        [Authorize(Roles = "professor")]
        public async Task<IActionResult> GenerateCode()
        {
            return Ok(new { roomCode = await GenerateUniqueRoomCode() });
        }

        [HttpPost]
        [Authorize(Roles = "professor")]
        public async Task<IActionResult> Create(RoomRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RoomName))
                return BadRequest("Room name is required.");

            string roomCode = NormalizeRoomCode(request.RoomCode);
            if (string.IsNullOrWhiteSpace(roomCode))
                return BadRequest("Generate room code first.");

            if (await _db.MultiplayerRooms.AnyAsync(r => r.RoomCode.ToLower() == roomCode))
                return BadRequest("Room code already exists. Please generate another code.");

            var room = new MultiplayerRoom
            {
                RoomCode = roomCode,
                RoomName = request.RoomName.Trim(),
                Status = "ongoing",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = GetUserId()
            };

            _db.MultiplayerRooms.Add(room);
            await _db.SaveChangesAsync();
            return Ok(ToRoomSummary(room));
        }

        [HttpPost("{roomId:int}/close")]
        [Authorize(Roles = "professor")]
        public async Task<IActionResult> Close(int roomId)
        {
            var room = await _db.MultiplayerRooms.SingleOrDefaultAsync(r => r.RoomId == roomId);
            if (room == null)
                return NotFound("Room was not found.");

            if (!CanAccessRoom(room))
                return Forbid();

            room.Status = "closed";
            await _db.SaveChangesAsync();
            return Ok(ToRoomSummary(room));
        }

        [HttpDelete("{roomId:int}")]
        [Authorize(Roles = "professor,admin")]
        public async Task<IActionResult> Delete(int roomId)
        {
            var room = await _db.MultiplayerRooms.SingleOrDefaultAsync(r => r.RoomId == roomId);
            if (room == null)
                return NotFound("Room was not found.");

            if (!CanAccessRoom(room))
                return Forbid();

            _db.MultiplayerRooms.Remove(room);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("join")]
        [Authorize(Roles = "student")]
        public async Task<IActionResult> Join(JoinRoomRequest request)
        {
            string roomCode = NormalizeRoomCode(request.RoomCode);
            if (string.IsNullOrWhiteSpace(roomCode))
                return BadRequest("Invalid room code");

            int userId = GetUserId();
            var profile = await _db.StudentProfiles.SingleOrDefaultAsync(p => p.UserId == userId);
            if (profile == null)
                return BadRequest("Student profile was not found.");

            var room = await _db.MultiplayerRooms
                .Include(r => r.Participants)
                .SingleOrDefaultAsync(r => r.RoomCode.ToLower() == roomCode);

            if (room == null)
                return NotFound("Invalid room code");

            if (string.Equals(room.Status, "closed", StringComparison.OrdinalIgnoreCase))
                return BadRequest("The room is closed");

            bool alreadyJoined = await _db.RoomParticipants
                .AnyAsync(p => p.RoomId == room.RoomId && p.UserId == profile.StudentId);

            if (!alreadyJoined)
            {
                var participant = new RoomParticipant
                {
                    RoomId = room.RoomId,
                    UserId = profile.StudentId,
                    JoinedAt = DateTime.UtcNow
                };

                _db.RoomParticipants.Add(participant);
                await _db.SaveChangesAsync();
            }

            return Ok(ToRoomJoin(room, profile.StudentId));
        }

        [HttpGet("{roomId:int}/participants")]
        [Authorize(Roles = "professor,admin,student")]
        public async Task<IActionResult> GetParticipants(int roomId)
        {
            var room = await _db.MultiplayerRooms
                .Include(r => r.Participants)
                    .ThenInclude(p => p.Student)
                        .ThenInclude(s => s!.User)
                .AsNoTracking()
                .SingleOrDefaultAsync(r => r.RoomId == roomId);

            if (room == null)
                return NotFound("Room was not found.");
            if (!CanAccessRoom(room))
                return Forbid();
            if (User.IsInRole("student") && !await IsRoomParticipantAsync(roomId, GetUserId()))
                return Forbid();

            return Ok(room.Participants.Select(participant => new
            {
                StudentId = participant.Student?.StudentId ?? 0,
                Name = participant.Student?.User == null
                    ? "Student"
                    : $"{participant.Student.User.FirstName} {participant.Student.User.LastName}".Trim(),
                Avatar = participant.Student?.User?.Avatar ?? ""
            }));
        }

        [HttpPost("{roomId:int}/quiz")]
        [Authorize(Roles = "professor")]
        public async Task<IActionResult> SaveQuiz(int roomId, RoomQuizRequest request)
        {
            var room = await _db.MultiplayerRooms.SingleOrDefaultAsync(r => r.RoomId == roomId);
            if (room == null)
                return NotFound("Room was not found.");
            if (!CanAccessRoom(room))
                return Forbid();
            if (User.IsInRole("student") && !await IsRoomParticipantAsync(roomId, GetUserId()))
                return Forbid();
            if (request.Questions == null || request.Questions.Count == 0)
                return BadRequest("At least one question is required.");

            int userId = GetUserId();
            var upload = await _db.ProfessorUploads
                .SingleOrDefaultAsync(item => item.RoomId == roomId);
            if (upload == null)
            {
                upload = new ProfessorUpload
                {
                    ProfessorId = userId,
                    CreatedBy = userId,
                    RoomId = roomId
                };
                _db.ProfessorUploads.Add(upload);
            }

            upload.FileName = request.FileName ?? "Room quiz";
            upload.TimerSeconds = Math.Max(1, request.TimerSeconds);
            upload.QuestionsJson = JsonSerializer.Serialize(request.Questions);
            upload.UploadedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("{roomId:int}/quiz")]
        [Authorize(Roles = "professor,admin,student")]
        public async Task<IActionResult> GetQuiz(int roomId)
        {
            var room = await _db.MultiplayerRooms.SingleOrDefaultAsync(r => r.RoomId == roomId);
            if (room == null)
                return NotFound("Room was not found.");
            if (!CanAccessRoom(room))
                return Forbid();

            var upload = await _db.ProfessorUploads
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.RoomId == roomId);
            if (upload == null)
                return NotFound("No quiz has been uploaded for this room.");

            List<RoomQuizQuestionRequest> questions;
            try
            {
                questions = JsonSerializer.Deserialize<List<RoomQuizQuestionRequest>>(upload.QuestionsJson) ?? new();
            }
            catch (JsonException)
            {
                return BadRequest("The room quiz data is invalid.");
            }

            for (int questionIndex = questions.Count - 1; questionIndex > 0; questionIndex--)
            {
                int swapIndex = Random.Shared.Next(questionIndex + 1);
                (questions[questionIndex], questions[swapIndex]) = (questions[swapIndex], questions[questionIndex]);
            }
            return Ok(new { TimerSeconds = upload.TimerSeconds, Questions = questions });
        }

        [HttpPost("{roomId:int}/results")]
        [Authorize(Roles = "student")]
        public async Task<IActionResult> SaveResult(int roomId, RoomResultRequest request)
        {
            if (request.Score < 0)
                return BadRequest("Score cannot be negative.");
            if (request.TotalQuestions < 0)
                return BadRequest("Total questions cannot be negative.");
            if (request.Score > request.TotalQuestions)
                return BadRequest("Score cannot exceed the number of questions.");

            var room = await _db.MultiplayerRooms.SingleOrDefaultAsync(r => r.RoomId == roomId);
            if (room == null)
                return NotFound("Room was not found.");
            if (!CanAccessRoom(room))
                return Forbid();

            int userId = GetUserId();
            if (!await IsRoomParticipantAsync(roomId, userId))
                return Forbid();
            var profile = await _db.StudentProfiles.SingleOrDefaultAsync(s => s.UserId == userId);
            if (profile == null)
                return BadRequest("Student profile was not found.");

            var result = await _db.RoomResults
                .SingleOrDefaultAsync(r => r.RoomId == roomId && r.StudentId == profile.StudentId);

            if (result == null)
            {
                result = new RoomResult { RoomId = roomId, StudentId = profile.StudentId };
                _db.RoomResults.Add(result);

                if (request.TotalQuestions > 0 && request.Score < request.TotalQuestions)
                {
                    profile.Lifespan = Math.Max(0, profile.Lifespan - 10);
                    profile.LifespanAutoRecoveryAt = profile.Lifespan == 0
                        ? DateTime.UtcNow.AddHours(3)
                        : null;
                    profile.LifespanWalkRecoveryAt = null;
                }
            }

            result.Score = request.Score;
            result.TotalQuestions = request.TotalQuestions;
            result.AnswersJson = JsonSerializer.Serialize(request.Answers ?? new());
            result.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(result);
        }

        [HttpGet("{roomId:int}/results/me")]
        [Authorize(Roles = "student")]
        public async Task<IActionResult> GetMyResult(int roomId)
        {
            int userId = GetUserId();
            if (!await IsRoomParticipantAsync(roomId, userId))
                return Forbid();
            var profile = await _db.StudentProfiles.SingleOrDefaultAsync(s => s.UserId == userId);
            if (profile == null)
                return BadRequest("Student profile was not found.");

            var result = await _db.RoomResults.AsNoTracking()
                .SingleOrDefaultAsync(item => item.RoomId == roomId && item.StudentId == profile.StudentId);
            if (result == null)
                return NotFound("Quiz result was not found.");

            return Ok(new
            {
                result.Score,
                result.TotalQuestions,
                Answers = JsonSerializer.Deserialize<List<RoomAnswerRequest>>(result.AnswersJson) ?? new()
            });
        }

        [HttpGet("{roomId:int}/results")]
        [Authorize(Roles = "professor,admin")]
        public async Task<IActionResult> GetResults(int roomId)
        {
            var room = await _db.MultiplayerRooms.SingleOrDefaultAsync(r => r.RoomId == roomId);
            if (room == null)
                return NotFound("Room was not found.");
            if (!CanAccessRoom(room))
                return Forbid();

            var results = await _db.RoomResults
                .Where(item => item.RoomId == roomId)
                .Include(item => item.Student)
                    .ThenInclude(student => student!.User)
                .AsNoTracking()
                .ToListAsync();

            return Ok(results.Select(result => new
            {
                StudentId = result.StudentId,
                StudentName = result.Student?.User == null
                    ? "Student"
                    : $"{result.Student.User.FirstName} {result.Student.User.LastName}".Trim(),
                result.Score,
                result.TotalQuestions
            }));
        }

        private async Task<string> GenerateUniqueRoomCode()
        {
			const string alphabet = "abcdefghijklmnopqrstuvwxyz123456789";
            string code;
            do
            {
                char[] characters = new char[10];
                for (int characterIndex = 0; characterIndex < characters.Length; characterIndex++)
                    characters[characterIndex] = alphabet[Random.Shared.Next(alphabet.Length)];
                code = new string(characters);
            }
            while (await _db.MultiplayerRooms.AnyAsync(r => r.RoomCode.ToLower() == code));

            return code;
        }

        private static string NormalizeRoomCode(string roomCode)
        {
            return (roomCode ?? "").Trim().ToLowerInvariant();
        }

        private async Task<bool> IsRoomParticipantAsync(int roomId, int userId)
        {
            var profile = await _db.StudentProfiles
                .AsNoTracking()
                .SingleOrDefaultAsync(student => student.UserId == userId);
            return profile != null && await _db.RoomParticipants.AnyAsync(participant =>
                participant.RoomId == roomId && participant.UserId == profile.StudentId);
        }

        private static object ToRoomSummary(MultiplayerRoom room)
        {
            return new
            {
                room.RoomId,
                room.RoomCode,
                room.RoomName,
                room.Status,
                StudentCount = room.Participants.Count
            };
        }

        private static object ToRoomJoin(MultiplayerRoom room, int studentId)
        {
            return new
            {
                room.RoomId,
                room.RoomCode,
                room.RoomName,
                room.Status,
                StudentCount = room.Participants.Count,
                StudentId = studentId
            };
        }

        private bool CanAccessRoom(MultiplayerRoom room)
        {
            string role = User.FindFirstValue("role") ?? "";
            if (string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role, "student", StringComparison.OrdinalIgnoreCase))
                return true;

            return room.CreatedBy == GetUserId();
        }

        private int GetUserId()
        {
            return int.TryParse(User.FindFirstValue("userId"), out int userId) ? userId : 0;
        }
    }

    public class RoomRequest
    {
        public string RoomName { get; set; } = "";

        public string RoomCode { get; set; } = "";
    }

    public class JoinRoomRequest
    {
        public string RoomCode { get; set; } = "";
    }

    public class RoomResultRequest
    {
        public int Score { get; set; }
        public int TotalQuestions { get; set; }
        public List<RoomAnswerRequest> Answers { get; set; } = new();
    }

    public class RoomQuizRequest
    {
        public string? FileName { get; set; }
        public int TimerSeconds { get; set; } = 30;
        public List<RoomQuizQuestionRequest> Questions { get; set; } = new();
    }

    public class RoomQuizQuestionRequest
    {
        public string Type { get; set; } = "";
        public string SubjectName { get; set; } = "";
        public string Question { get; set; } = "";
        public string ChoiceA { get; set; } = "";
        public string ChoiceB { get; set; } = "";
        public string ChoiceC { get; set; } = "";
        public string ChoiceD { get; set; } = "";
        public string CorrectAnswer { get; set; } = "";
    }

    public class RoomAnswerRequest : RoomQuizQuestionRequest
    {
        public string StudentAnswer { get; set; } = "";
        public bool IsCorrect { get; set; }
    }
}
