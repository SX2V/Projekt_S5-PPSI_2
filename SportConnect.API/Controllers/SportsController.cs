using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
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

        public SportsController(AppDbContext context, IActionLogger actionLogger, IStringLocalizer<SportsController> localizer)
        {
            _context = context;
            _actionLogger = actionLogger;
            _localizer = localizer;
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<Sport>>> GetAllSports()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var currentUserId))
                return Unauthorized(new { message = _localizer["InvalidUserIdentifier"] });

            var sports = await _context.Sports.ToListAsync();

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

            await _actionLogger.LogAsync(adminId, $"admin created sport {sport.Id} ({sport.Name})");

            return CreatedAtAction(nameof(GetAllSports), new { id = sport.Id }, sport);
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
            {
                return NotFound(new { message = _localizer["SportNotFound"] });
            }

            _context.Sports.Remove(sport);
            await _context.SaveChangesAsync();

            await _actionLogger.LogAsync(adminId, $"admin deleted sport {id}");

            return NoContent();
        }
    }
}
