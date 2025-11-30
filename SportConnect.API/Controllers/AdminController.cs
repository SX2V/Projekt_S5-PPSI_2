using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportConnect.API.Data;
using SportConnect.API.Dtos;
using SportConnect.API.Models;
using SportConnect.API.Services;
using System.Security.Claims;

namespace SportConnect.API.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IActionLogger _actionLogger;

        public AdminController(AppDbContext context, IActionLogger actionLogger)
        {
            _context = context;
            _actionLogger = actionLogger;
        }

        [HttpGet("statistics")]
        public async Task<ActionResult<TrainingRequestStatsDto>> GetTrainingRequestStats()
        {
            var adminIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(adminIdClaim, out var adminId))
                return Unauthorized();

            var now = DateTime.UtcNow;

            var dailyRequests = await _context.TrainingRequests
                .CountAsync(r => r.CreatedAt >= now.AddDays(-1));

            var weeklyRequests = await _context.TrainingRequests
                .CountAsync(r => r.CreatedAt >= now.AddDays(-7));

            var monthlyRequests = await _context.TrainingRequests
                .CountAsync(r => r.CreatedAt >= now.AddDays(-30));

            var dailyResponses = await _context.TrainingRequests
                .CountAsync(r => r.RespondedAt != null && r.RespondedAt >= now.AddDays(-1));

            var weeklyResponses = await _context.TrainingRequests
                .CountAsync(r => r.RespondedAt != null && r.RespondedAt >= now.AddDays(-7));

            var monthlyResponses = await _context.TrainingRequests
                .CountAsync(r => r.RespondedAt != null && r.RespondedAt >= now.AddDays(-30));

            var stats = new TrainingRequestStatsDto
            {
                DailyRequests = dailyRequests,
                WeeklyRequests = weeklyRequests,
                MonthlyRequests = monthlyRequests,
                DailyResponses = dailyResponses,
                WeeklyResponses = weeklyResponses,
                MonthlyResponses = monthlyResponses
            };

            await _actionLogger.LogAsync(adminId, "admin viewed training request statistics");

            return Ok(stats);
        }

        [HttpGet("logs")]
        public async Task<IActionResult> GetActionLogs()
        {
            var adminIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(adminIdClaim, out var adminId))
                return Unauthorized();

            var logs = await _context.ActionLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(100) 
                .ToListAsync();

            await _actionLogger.LogAsync(adminId, "admin viewed action logs");

            return Ok(logs);
        }
    }
}
