using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportConnect.API.Data;
using SportConnect.API.Dtos;
using SportConnect.API.Models;

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

    }

}
