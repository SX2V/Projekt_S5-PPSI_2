using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SportConnect.API.Data;
using SportConnect.API.Models;
using SportConnect.API.Dtos;

namespace SportConnect.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TrainingRequestsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IStringLocalizer<TrainingRequestsController> _localizer;


        public TrainingRequestsController(AppDbContext context, IStringLocalizer<TrainingRequestsController> localizer)
        {
            _context = context;
            _localizer = localizer;

        }

        private async Task LogAction(Guid userId, string action)
        {
            var log = new ActionLog
            {
                UserId = userId,
                Action = action,
                Timestamp = DateTime.UtcNow
            };
            _context.ActionLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateTrainingRequestDto dto)
        {
            var senderIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(senderIdClaim, out var senderId))
                return Unauthorized(new { message = _localizer["InvalidUserIdentifier"] });

            if (senderId == dto.ReceiverId)
                return BadRequest(new { message = _localizer["CannotSendRequestToYourself"] });

            var receiverExists = await _context.Users.AnyAsync(u => u.Id == dto.ReceiverId);
            if (!receiverExists)
                return NotFound(new { message = _localizer["ReceiverNotFound"] });

            var request = new TrainingRequest
            {
                Id = Guid.NewGuid(),
                SenderId = senderId,
                ReceiverId = dto.ReceiverId,
                CreatedAt = DateTime.UtcNow,
                Status = TrainingRequestStatus.Pending,
                TrainingDateTime = dto.TrainingDateTime,
                Location = dto.Location,
                Message = dto.Message
            };

            _context.TrainingRequests.Add(request);
            await _context.SaveChangesAsync();

            await LogAction(senderId, $"sent training request to {dto.ReceiverId}");

            return CreatedAtAction(nameof(GetById), new { id = request.Id }, request);
        }

        [HttpPatch("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateTrainingRequestStatusDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var currentUserId))
                return Unauthorized(new { message = _localizer["InvalidUserIdentifier"] });

            var request = await _context.TrainingRequests.FindAsync(id);
            if (request == null)
                return NotFound(new { message = _localizer["TrainingRequestNotFound"] });

            if (request.ReceiverId != currentUserId)
                return Forbid(_localizer["OnlyReceiverCanRespond"]);

            if (request.Status != TrainingRequestStatus.Pending)
                return Conflict(new { message = _localizer["TrainingRequestAlreadyProcessed"] });

            if (dto.Status != TrainingRequestStatus.Accepted && dto.Status != TrainingRequestStatus.Rejected)
                return BadRequest(new { message = _localizer["TrainingRequestInvalidStatus"] });

            request.Status = dto.Status;
            request.RespondedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await LogAction(currentUserId, $"responded to training request {id} with status {dto.Status}");

            return Ok(request);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(Guid id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var currentUserId))
                return Unauthorized(new { message = _localizer["InvalidUserIdentifier"] });

            var request = await _context.TrainingRequests
                .Include(tr => tr.Sender)
                .Include(tr => tr.Receiver)
                .FirstOrDefaultAsync(tr => tr.Id == id);

            if (request == null)
                return NotFound(new { message = _localizer["TrainingRequestNotFound"] });

            if (request.SenderId != currentUserId && request.ReceiverId != currentUserId)
                return Forbid(_localizer["NotAuthorizedToViewRequest"]);

            await LogAction(currentUserId, $"viewed training request {id}");

            return Ok(request);
        }

        [HttpGet("me/sent")]
        [Authorize]
        public async Task<IActionResult> GetMySent()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var currentUserId))
                return Unauthorized(new { message = _localizer["InvalidUserIdentifier"] });

            var requests = await _context.TrainingRequests
                .Where(tr => tr.SenderId == currentUserId)
                .Include(tr => tr.Receiver)
                .OrderByDescending(tr => tr.CreatedAt)
                .ToListAsync();

            await LogAction(currentUserId, "viewed own sent training requests");

            return Ok(requests);
        }

        [HttpGet("me/received")]
        [Authorize]
        public async Task<IActionResult> GetMyReceived()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var currentUserId))
                return Unauthorized(new { message = _localizer["InvalidUserIdentifier"] });

            var requests = await _context.TrainingRequests
                .Where(tr => tr.ReceiverId == currentUserId)
                .Include(tr => tr.Sender)
                .OrderByDescending(tr => tr.CreatedAt)
                .ToListAsync();

            await LogAction(currentUserId, "viewed own received training requests");

            return Ok(requests);
        }
        [HttpPatch("{id}/cancel")]
        [Authorize]
        public async Task<IActionResult> CancelTrainingRequest(Guid id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var currentUserId))
                return Unauthorized(new { message = _localizer["InvalidUserIdentifier"] });

            var request = await _context.TrainingRequests.FindAsync(id);
            if (request == null)
                return NotFound(new { message = _localizer["TrainingRequestNotFound"] });

            if (request.SenderId != currentUserId && request.ReceiverId != currentUserId)
                return Forbid(_localizer["NotAuthorizedToModifyRequest"]);

            request.Status = TrainingRequestStatus.Cancelled;
            request.RespondedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await LogAction(currentUserId, $"cancelled training request {id}");

            return NoContent();
        }

    }
}
