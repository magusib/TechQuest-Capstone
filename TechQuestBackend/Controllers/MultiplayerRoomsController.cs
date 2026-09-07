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
                .SingleOrDefaultAsync(r => r.RoomCode.ToUpper() == normalizedCode);

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

            if (await _db.MultiplayerRooms.AnyAsync(r => r.RoomCode.ToUpper() == roomCode))
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
                .SingleOrDefaultAsync(r => r.RoomCode.ToUpper() == roomCode);

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

        [HttpPost("{roomId:int}/results")]
        [Authorize(Roles = "professor")]
        public async Task<IActionResult> SaveResult(int roomId, RoomResultRequest request)
        {
            if (request.Score < 0)
                return BadRequest("Score cannot be negative.");

            if (!await _db.MultiplayerRooms.AnyAsync(r => r.RoomId == roomId))
                return NotFound("Room was not found.");

            if (!await _db.StudentProfiles.AnyAsync(s => s.StudentId == request.StudentId))
                return BadRequest("Student profile was not found.");

            var result = await _db.RoomResults
                .SingleOrDefaultAsync(r => r.RoomId == roomId && r.StudentId == request.StudentId);

            if (result == null)
            {
                result = new RoomResult { RoomId = roomId, StudentId = request.StudentId };
                _db.RoomResults.Add(result);
            }

            result.Score = request.Score;
            result.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(result);
        }

        private async Task<string> GenerateUniqueRoomCode()
        {
            string code;
            do
            {
                code = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            }
            while (await _db.MultiplayerRooms.AnyAsync(r => r.RoomCode == code));

            return code;
        }

        private static string NormalizeRoomCode(string roomCode)
        {
            return (roomCode ?? "").Trim().ToUpperInvariant();
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
        public int StudentId { get; set; }
        public int Score { get; set; }
    }
}
