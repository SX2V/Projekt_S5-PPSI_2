using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportConnect.API.Data;
using SportConnect.API.Models;

namespace SportConnect.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TrainingRequestsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TrainingRequestsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateTrainingRequestDto dto)
        {
            var senderIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(senderIdClaim, out var senderId))
                return Unauthorized();

            if (senderId == dto.ReceiverId)
                return BadRequest("You cannot send a training request to yourself.");

            var receiverExists = await _context.Users.AnyAsync(u => u.Id == dto.ReceiverId);
            if (!receiverExists)
                return NotFound("Receiver user not found.");

            var request = new TrainingRequest
            {
                Id = Guid.NewGuid(),
                SenderId = senderId,
                ReceiverId = dto.ReceiverId,
                CreatedAt = DateTime.UtcNow,
                Status = "Pending"
            };

            _context.TrainingRequests.Add(request);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = request.Id }, request);
        }

        [HttpPatch("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateTrainingRequestStatusDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var currentUserId))
                return Unauthorized();

            var request = await _context.TrainingRequests.FindAsync(id);
            if (request == null)
                return NotFound("Training request not found.");

            if (request.ReceiverId != currentUserId)
                return Forbid("Only the receiver can respond to the request.");

            if (request.Status != "Pending")
                return Conflict("The request has already been processed.");

            if (dto.Status != "Accepted" && dto.Status != "Rejected")
                return BadRequest("Status must be either Accepted or Rejected.");

            request.Status = dto.Status;
            await _context.SaveChangesAsync();

            return Ok(request);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(Guid id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var currentUserId))
                return Unauthorized();

            var request = await _context.TrainingRequests
                .Include(tr => tr.Sender)
                .Include(tr => tr.Receiver)
                .FirstOrDefaultAsync(tr => tr.Id == id);

            if (request == null)
                return NotFound("Training request not found.");

            if (request.SenderId != currentUserId && request.ReceiverId != currentUserId)
                return Forbid("You are not authorized to view this request.");

            return Ok(request);
        }

        [HttpGet("me/sent")]
        [Authorize]
        public async Task<IActionResult> GetMySent()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var currentUserId))
                return Unauthorized();

            var requests = await _context.TrainingRequests
                .Where(tr => tr.SenderId == currentUserId)
                .Include(tr => tr.Receiver)
                .OrderByDescending(tr => tr.CreatedAt)
                .ToListAsync();

            return Ok(requests);
        }

        [HttpGet("me/received")]
        [Authorize]
        public async Task<IActionResult> GetMyReceived()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var currentUserId))
                return Unauthorized();

            var requests = await _context.TrainingRequests
                .Where(tr => tr.ReceiverId == currentUserId)
                .Include(tr => tr.Sender)
                .OrderByDescending(tr => tr.CreatedAt)
                .ToListAsync();

            return Ok(requests);
        }
    }
}
