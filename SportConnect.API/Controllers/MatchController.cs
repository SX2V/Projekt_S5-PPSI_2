using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportConnect.API.Data;
using SportConnect.API.Dtos;
using SportConnect.API.Models;
using System.Security.Claims;

namespace SportConnect.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MatchController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MatchController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("request")]
        [Authorize]
        public async Task<IActionResult> SendMatchRequest([FromBody] MatchRequestDto dto)
        {
            var fromUser = await _context.Users.FindAsync(dto.FromUserId);
            var toUser = await _context.Users.FindAsync(dto.ToUserId);
            var sport = await _context.Sports.FindAsync(dto.SportId);

            if (fromUser == null || toUser == null || sport == null)
                return BadRequest("Invalid user or sport ID.");

            var request = new MatchRequest
            {
                FromUserId = dto.FromUserId,
                ToUserId = dto.ToUserId,
                SportId = dto.SportId
            };

            _context.MatchRequests.Add(request);
            await _context.SaveChangesAsync();

            return Ok(request.Id);
        }
        [HttpGet("incoming")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<MatchRequestViewDto>>> GetIncomingRequests([FromQuery] Guid userId)
        {
            var requests = await _context.MatchRequests
                .Where(r => r.ToUserId == userId)
                .Include(r => r.Sport)
                .Select(r => new MatchRequestViewDto
                {
                    Id = r.Id,
                    FromUserId = r.FromUserId,
                    ToUserId = r.ToUserId,
                    SportId = r.SportId,
                    SportName = r.Sport.Name ?? string.Empty,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return Ok(requests);
        }

        [HttpGet("outgoing")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<MatchRequestViewDto>>> GetOutgoingRequests([FromQuery] Guid userId)
        {
            var requests = await _context.MatchRequests
                .Where(r => r.FromUserId == userId)
                .Include(r => r.Sport)
                .Select(r => new MatchRequestViewDto
                {
                    Id = r.Id,
                    FromUserId = r.FromUserId,
                    ToUserId = r.ToUserId,
                    SportId = r.SportId,
                    SportName = r.Sport.Name ?? string.Empty,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return Ok(requests);
        }
        [HttpPatch("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateMatchRequestStatus(Guid id, [FromBody] MatchRequestStatusDto dto)
        {
            var request = await _context.MatchRequests.FindAsync(id);

            if (request == null)
                return NotFound("Match request not found.");

            if (dto.Status != "Accepted" && dto.Status != "Rejected")
                return BadRequest("Invalid status. Use 'Accepted' or 'Rejected'.");

            request.Status = dto.Status;
            await _context.SaveChangesAsync();

            return Ok($"Match request {id} updated to '{dto.Status}'.");
        }
        [HttpGet("history")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<MatchRequestViewDto>>> GetAcceptedRequests([FromQuery] Guid userId)
        {
            var requests = await _context.MatchRequests
                .Where(r => (r.FromUserId == userId || r.ToUserId == userId) && r.Status == "Accepted")
                .Include(r => r.Sport)
                .Select(r => new MatchRequestViewDto
                {
                    Id = r.Id,
                    FromUserId = r.FromUserId,
                    ToUserId = r.ToUserId,
                    SportId = r.SportId,
                    SportName = r.Sport.Name ?? string.Empty,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return Ok(requests);
        }
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteMatchRequest(Guid id)
        {
            var request = await _context.MatchRequests.FindAsync(id);

            if (request == null)
                return NotFound("Match request not found.");

            _context.MatchRequests.Remove(request);
            await _context.SaveChangesAsync();

            return Ok($"Match request {id} has been deleted.");
        }
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMyMatches()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Invalid token.");

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized("Invalid user identifier.");

            var me = await _context.Users
                .Include(u => u.UserSports)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (me == null)
                return NotFound("User not found.");

            var mySportIds = me.UserSports.Select(us => us.SportId).ToHashSet();

            var candidates = await _context.Users
                .Where(u => u.Id != me.Id && u.IsAvailableNow)
                .Include(u => u.UserSports)
                .ToListAsync();

            var results = candidates
                .Select(u =>
                {
                    var candidateSportIds = u.UserSports.Select(us => us.SportId).ToHashSet();
                    var sharedSports = mySportIds.Intersect(candidateSportIds).ToList();

                    if (sharedSports.Count == 0)
                        return null;

                    var distanceKm = HaversineDistanceKm(me.Latitude, me.Longitude, u.Latitude, u.Longitude);

                    if (distanceKm > me.SearchRadiusKm)
                        return null;

                    return new
                    {
                        u.Id,
                        u.Name,
                        DistanceKm = Math.Round(distanceKm, 2),
                        SharedSportIds = sharedSports
                    };
                })
                .Where(x => x != null)
                .ToList();

            return Ok(results);
        }
        private static double HaversineDistanceKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0;
            double ToRad(double angle) => Math.PI * angle / 180.0;

            var dLat = ToRad(lat2 - lat1);
            var dLon = ToRad(lon2 - lon1);
            var a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

    }

}
