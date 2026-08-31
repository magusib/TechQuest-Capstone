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
            return Ok(await _db.MultiplayerRooms.AsNoTracking().OrderByDescending(r => r.CreatedAt).ToListAsync());
        }

        [HttpPost]
        [Authorize(Roles = "professor")]
        public async Task<IActionResult> Create(RoomRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RoomName))
                return BadRequest("Room name is required.");

            string roomCode = await GenerateUniqueRoomCode();

            var room = new MultiplayerRoom
            {
                RoomCode = roomCode,
                RoomName = request.RoomName.Trim(),
                Status = "waiting",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = GetUserId()
            };

            _db.MultiplayerRooms.Add(room);
            await _db.SaveChangesAsync();
            return Ok(room);
        }

        [HttpPost("join")]
        [Authorize(Roles = "student")]
        public async Task<IActionResult> Join(JoinRoomRequest request)
        {
            int userId = GetUserId();
            var profile = await _db.StudentProfiles.SingleOrDefaultAsync(p => p.UserId == userId);
            if (profile == null)
                return BadRequest("Student profile was not found.");

            var room = await _db.MultiplayerRooms
                .SingleOrDefaultAsync(r => r.RoomCode == request.RoomCode.Trim());

            if (room == null)
                return NotFound("Room was not found.");

            if (await _db.RoomParticipants.AnyAsync(p => p.RoomId == room.RoomId && p.StudentId == profile.StudentId))
                return BadRequest("Student already joined this room.");

            var participant = new RoomParticipant
            {
                RoomId = room.RoomId,
                StudentId = profile.StudentId,
                JoinedAt = DateTime.UtcNow
            };

            _db.RoomParticipants.Add(participant);
            await _db.SaveChangesAsync();
            return Ok(participant);
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

        private int GetUserId()
        {
            return int.TryParse(User.FindFirstValue("userId"), out int userId) ? userId : 0;
        }
    }

    public class RoomRequest
    {
        public string RoomName { get; set; } = "";
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
