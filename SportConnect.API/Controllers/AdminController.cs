using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportConnect.API.Data;
using SportConnect.API.Dtos;

namespace SportConnect.API.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("statistics")]
        public async Task<ActionResult<TrainingRequestStatsDto>> GetTrainingRequestStats()
        {
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

            return Ok(stats);
        }
    }
}
