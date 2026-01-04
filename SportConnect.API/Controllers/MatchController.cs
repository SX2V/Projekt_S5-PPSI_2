using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Caching.Memory;
using SportConnect.API.Data;
using SportConnect.API.Dtos;
using SportConnect.API.Models;
using SportConnect.API.Services;
using System.Security.Claims;

namespace SportConnect.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MatchController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IActionLogger _actionLogger;
        private readonly IStringLocalizer<MatchController> _localizer;
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan MatchesCacheDuration = TimeSpan.FromMinutes(2);

        public MatchController(AppDbContext context, IActionLogger actionLogger, IStringLocalizer<MatchController> localizer, IMemoryCache cache)
        {
            _context = context;
            _actionLogger = actionLogger;
            _localizer = localizer;
            _cache = cache;
        }

        [HttpPost("request")]
        [Authorize]
        public async Task<IActionResult> SendMatchRequest([FromBody] MatchRequestDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var fromUserId))
                return Unauthorized(new { message = _localizer["InvalidUserIdentifier"] });

            var fromUser = await _context.Users.FindAsync(dto.FromUserId);
            var toUser = await _context.Users.FindAsync(dto.ToUserId);
            var sport = await _context.Sports.FindAsync(dto.SportId);

            if (fromUser == null || toUser == null || sport == null)
                return BadRequest(new { message = _localizer["InvalidUserOrSportId"] });

            var request = new MatchRequest
            {
                FromUserId = dto.FromUserId,
                ToUserId = dto.ToUserId,
                SportId = dto.SportId,
                Status = MatchRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.MatchRequests.Add(request);
            await _context.SaveChangesAsync();

            await _actionLogger.LogAsync(fromUserId, $"user {fromUserId} sent match request to {dto.ToUserId} for sport {dto.SportId}");

            _cache.Remove($"matches:{dto.FromUserId}");
            _cache.Remove($"matches:{dto.ToUserId}");

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
                    Status = r.Status.ToString(),
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            await _actionLogger.LogAsync(userId, $"user {userId} viewed incoming match requests");

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
                    Status = r.Status.ToString(),
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            await _actionLogger.LogAsync(userId, $"user {userId} viewed outgoing match requests");

            return Ok(requests);
        }

        [HttpPatch("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateMatchRequestStatus(Guid id, [FromBody] MatchRequestStatusDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { message = _localizer["InvalidUserIdentifier"] });

            var request = await _context.MatchRequests.FindAsync(id);

            if (request == null)
                return NotFound(new { message = _localizer["MatchRequestNotFound"] });

            if (dto.Status != MatchRequestStatus.Accepted && dto.Status != MatchRequestStatus.Rejected)
                return BadRequest(new { message = _localizer["InvalidMatchRequestStatus"] });

            request.Status = dto.Status;
            await _context.SaveChangesAsync();

            await _actionLogger.LogAsync(userId, $"user {userId} updated match request {id} to {dto.Status}");

            _cache.Remove($"matches:{request.FromUserId}");
            _cache.Remove($"matches:{request.ToUserId}");

            return Ok(new { message = _localizer["MatchRequestUpdated", id, dto.Status] });
        }

        [HttpGet("history")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<MatchRequestViewDto>>> GetAcceptedRequests([FromQuery] Guid userId)
        {
            var requests = await _context.MatchRequests
                .Where(r => (r.FromUserId == userId || r.ToUserId == userId) && r.Status == MatchRequestStatus.Accepted)
                .Include(r => r.Sport)
                .Select(r => new MatchRequestViewDto
                {
                    Id = r.Id,
                    FromUserId = r.FromUserId,
                    ToUserId = r.ToUserId,
                    SportId = r.SportId,
                    SportName = r.Sport.Name ?? string.Empty,
                    Status = r.Status.ToString(),
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            await _actionLogger.LogAsync(userId, $"user {userId} viewed match history");

            return Ok(requests);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteMatchRequest(Guid id)
        {
            var adminIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(adminIdClaim, out var adminId))
                return Unauthorized(new { message = _localizer["InvalidUserIdentifier"] });

            var request = await _context.MatchRequests.FindAsync(id);

            if (request == null)
                return NotFound(new { message = _localizer["MatchRequestNotFound"] });

            _context.MatchRequests.Remove(request);
            await _context.SaveChangesAsync();

            await _actionLogger.LogAsync(adminId, $"admin deleted match request {id}");

            _cache.Remove($"matches:{request.FromUserId}");
            _cache.Remove($"matches:{request.ToUserId}");

            return Ok(new { message = _localizer["MatchRequestDeleted", id] });
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMyMatches()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { message = _localizer["InvalidToken"] });

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { message = _localizer["InvalidUserIdentifier"] });

            var cacheKey = $"matches:{userId}";

            if (!_cache.TryGetValue(cacheKey, out List<UserMatchDto>? results))
            {
                var me = await _context.Users
                    .Include(u => u.UserSports)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (me == null)
                    return NotFound(new { message = _localizer["UserNotFound"] });

                var mySportIds = me.UserSports.Select(us => us.SportId).ToHashSet();

                var candidates = await _context.Users
                    .Where(u => u.Id != me.Id && u.IsAvailableNow)
                    .Include(u => u.UserSports)
                    .ToListAsync();

                results = candidates
                    .Select(u =>
                    {
                        var candidateSportIds = u.UserSports.Select(us => us.SportId).ToHashSet();
                        var sharedSports = mySportIds.Intersect(candidateSportIds).ToList();

                        if (sharedSports.Count == 0)
                            return null;

                        var distanceKm = HaversineDistanceKm(me.Latitude, me.Longitude, u.Latitude, u.Longitude);

                        if (distanceKm > me.SearchRadiusKm)
                            return null;

                        return new UserMatchDto
                        {
                            Id = u.Id,
                            Name = u.Name ?? string.Empty,
                            Email = u.Email ?? string.Empty,
                            IsAvailableNow = u.IsAvailableNow,
                            SearchRadiusKm = u.SearchRadiusKm,
                            DistanceKm = Math.Round(distanceKm, 2),
                            SharedSportIds = sharedSports
                        };
                    })
                    .Where(x => x != null)
                    .ToList()!;

                _cache.Set(cacheKey, results, MatchesCacheDuration);
            }

            await _actionLogger.LogAsync(userId, $"user {userId} viewed own matches");

            return Ok(results);
        }

        [HttpPatch("{id}/cancel")]
        [Authorize]
        public async Task<IActionResult> CancelMatchRequest(Guid id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { message = _localizer["InvalidUserIdentifier"] });

            var request = await _context.MatchRequests.FindAsync(id);

            if (request == null)
                return NotFound(new { message = _localizer["MatchRequestNotFound"] });

            if (request.FromUserId != userId && request.ToUserId != userId)
                return Forbid();

            request.Status = MatchRequestStatus.Cancelled;
            await _context.SaveChangesAsync();

            await _actionLogger.LogAsync(userId, $"user {userId} cancelled match request {id}");

            _cache.Remove($"matches:{request.FromUserId}");
            _cache.Remove($"matches:{request.ToUserId}");

            return NoContent();
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