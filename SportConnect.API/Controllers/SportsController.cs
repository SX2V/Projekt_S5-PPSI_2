using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Caching.Memory;
using SportConnect.API.Data;
using SportConnect.API.Models;
using SportConnect.API.Services;
using System.Security.Claims;

namespace SportConnect.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SportsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IActionLogger _actionLogger;
        private readonly IStringLocalizer<SportsController> _localizer;
        private readonly IMemoryCache _cache;

        private static readonly TimeSpan AllSportsCacheDuration = TimeSpan.FromHours(1);

        public SportsController(
            AppDbContext context,
            IActionLogger actionLogger,
            IStringLocalizer<SportsController> localizer,
            IMemoryCache cache)
        {
            _context = context;
            _actionLogger = actionLogger;
            _localizer = localizer;
            _cache = cache;
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<Sport>>> GetAllSports()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var currentUserId))
                return Unauthorized(new { message = _localizer["InvalidUserIdentifier"] });

            var cacheKey = CacheKeys.AllSports();

            if (!_cache.TryGetValue(cacheKey, out List<Sport>? sports))
            {
                sports = await _context.Sports.ToListAsync();
                _cache.Set(cacheKey, sports, AllSportsCacheDuration);
            }

            await _actionLogger.LogAsync(currentUserId, "viewed all sports");

            return Ok(sports);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Sport>> CreateSport(Sport sport)
        {
            var adminIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(adminIdClaim, out var adminId))
                return Unauthorized(new { message = _localizer["InvalidUserIdentifier"] });

            _context.Sports.Add(sport);
            await _context.SaveChangesAsync();

            _cache.Remove(CacheKeys.AllSports());

            await _actionLogger.LogAsync(adminId, $"admin created sport {sport.Id} ({sport.Name})");

            return CreatedAtAction(nameof(GetAllSports), new { id = sport.Id }, sport);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateSport(Guid id, Sport updatedSport)
        {
            var adminIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(adminIdClaim, out var adminId))
                return Unauthorized(new { message = _localizer["InvalidUserIdentifier"] });

            var sport = await _context.Sports.FindAsync(id);
            if (sport == null)
                return NotFound(new { message = _localizer["SportNotFound"] });

            sport.Name = updatedSport.Name;
            sport.Description = updatedSport.Description;
            sport.TypicalDistanceKm = updatedSport.TypicalDistanceKm;

            await _context.SaveChangesAsync();

            _cache.Remove(CacheKeys.AllSports());

            await _actionLogger.LogAsync(adminId, $"admin updated sport {id}");

            return Ok(sport);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteSport(Guid id)
        {
            var adminIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(adminIdClaim, out var adminId))
                return Unauthorized(new { message = _localizer["InvalidUserIdentifier"] });

            var sport = await _context.Sports.FindAsync(id);
            if (sport == null)
                return NotFound(new { message = _localizer["SportNotFound"] });

            _context.Sports.Remove(sport);
            await _context.SaveChangesAsync();

            _cache.Remove(CacheKeys.AllSports());

            await _actionLogger.LogAsync(adminId, $"admin deleted sport {id}");

            return NoContent();
        }
    }
}
