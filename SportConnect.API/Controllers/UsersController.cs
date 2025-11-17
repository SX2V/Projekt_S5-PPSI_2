using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportConnect.API.Data;
using SportConnect.API.Dtos;
using SportConnect.API.Models;
using System.Security.Claims;
using System.Text.Json;

namespace SportConnect.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<User>> CreateUser([FromBody] User user)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            bool emailExists = await _context.Users.AnyAsync(u => u.Email == user.Email);
            if (emailExists)
            {
                return Conflict(new { message = "Email already exists." });
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<User>> GetUserById(Guid id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] User updatedUser)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingUser = await _context.Users.FindAsync(id);

            if (existingUser == null)
            {
                return NotFound();
            }

            existingUser.Name = updatedUser.Name;
            existingUser.Email = updatedUser.Email;
            existingUser.IsAvailableNow = updatedUser.IsAvailableNow;
            existingUser.SearchRadiusKm = updatedUser.SearchRadiusKm;

            await _context.SaveChangesAsync();

            return Ok(existingUser);
        }

        [HttpPatch("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PatchUser(Guid id, [FromBody] JsonElement updates)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            if (updates.TryGetProperty("name", out var nameProp))
            {
                user.Name = nameProp.GetString();
            }

            if (updates.TryGetProperty("email", out var emailProp))
            {
                user.Email = emailProp.GetString();
            }

            if (updates.TryGetProperty("isAvailableNow", out var availableProp))
            {
                user.IsAvailableNow = availableProp.GetBoolean();
            }

            if (updates.TryGetProperty("searchRadiusKm", out var radiusProp))
            {
                user.SearchRadiusKm = radiusProp.GetInt32();
            }

            await _context.SaveChangesAsync();

            return Ok(user);
        }
        [HttpPost("{id}/sports")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignSportsToUser(Guid id, AssignSportsDto dto)
        {
            var user = await _context.Users
                .Include(u => u.UserSports)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound();

            user.UserSports.Clear();

            foreach (var sportId in dto.SportIds)
            {
                var sportExists = await _context.Sports.AnyAsync(s => s.Id == sportId);
                if (!sportExists)
                    return BadRequest($"Sport with ID {sportId} does not exist.");

                user.UserSports.Add(new UserSport
                {
                    UserId = user.Id,
                    SportId = sportId
                });
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("{id}/sports")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<UserSportDto>>> GetUserSports(Guid id)
        {
            var user = await _context.Users
                .Include(u => u.UserSports)
                .ThenInclude(us => us.Sport)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound();

            var sports = user.UserSports.Select(us => new UserSportDto
            {
                SportId = us.Sport.Id,
                Name = us.Sport.Name,
                Description = us.Sport.Description,
                TypicalDistanceKm = us.Sport.TypicalDistanceKm
            });

            return Ok(sports);
        }
        [HttpGet("{id}/assigned-sports")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<UserSportDto>>> GetUserAssignedSports(Guid id)
        {
            var user = await _context.Users
                .Include(u => u.UserSports)
                .ThenInclude(us => us.Sport)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound();

            var sports = user.UserSports.Select(us => new UserSportDto
            {
                SportId = us.Sport.Id,
                Name = us.Sport.Name,
                Description = us.Sport.Description,
                TypicalDistanceKm = us.Sport.TypicalDistanceKm
            });

            return Ok(sports);
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<UserMatchDto>>> GetUsersBySport(
        [FromQuery] Guid sportId,
        [FromQuery] bool? isAvailableNow,
        [FromQuery] int? maxSearchRadiusKm)
        {
            if (sportId == Guid.Empty)
                return BadRequest("Invalid sportId.");

            var query = _context.Users
                .Include(u => u.UserSports)
                .Where(u => u.UserSports.Any(us => us.SportId == sportId));

            if (isAvailableNow.HasValue)
                query = query.Where(u => u.IsAvailableNow == isAvailableNow.Value);

            if (maxSearchRadiusKm.HasValue)
                query = query.Where(u => u.SearchRadiusKm <= maxSearchRadiusKm.Value);

            var users = await query
                .Select(u => new UserMatchDto
                {
                    Id = u.Id,
                    Name = u.Name ?? string.Empty,
                    Email = u.Email ?? string.Empty,
                    IsAvailableNow = u.IsAvailableNow,
                    SearchRadiusKm = u.SearchRadiusKm
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("match")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<UserMatchDto>>> GetMatches([FromQuery] Guid userId)
        {
            var currentUser = await _context.Users
                .Include(u => u.UserSports)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (currentUser == null)
                return NotFound("User not found.");

            var sportIds = currentUser.UserSports.Select(us => us.SportId).ToList();

            var matches = await _context.Users
                .Where(u =>
                    u.Id != userId &&
                    u.IsAvailableNow &&
                    u.SearchRadiusKm <= currentUser.SearchRadiusKm &&
                    u.UserSports.Any(us => sportIds.Contains(us.SportId)))
                .Select(u => new UserMatchDto
                {
                    Id = u.Id,
                    Name = u.Name ?? string.Empty,
                    Email = u.Email ?? string.Empty,
                    IsAvailableNow = u.IsAvailableNow,
                    SearchRadiusKm = u.SearchRadiusKm
                })
                .ToListAsync();

            return Ok(matches);
        }

        [HttpPatch("{id}/block")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BlockUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            user.IsBlocked = true;
            await _context.SaveChangesAsync();

            return Ok($"User {id} has been blocked.");
        }

        [HttpPatch("{id}/unblock")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UnblockUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            user.IsBlocked = false;
            await _context.SaveChangesAsync();

            return Ok($"User {id} has been unblocked.");
        }

        [HttpPatch("me/profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Invalid token.");

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized("Invalid user identifier.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound();

            if (!string.IsNullOrEmpty(dto.Name)) user.Name = dto.Name;
            if (!string.IsNullOrEmpty(dto.Email)) user.Email = dto.Email;
            if (dto.Age.HasValue) user.Age = dto.Age.Value;
            if (!string.IsNullOrEmpty(dto.Description)) user.Description = dto.Description;

            await _context.SaveChangesAsync();
            return Ok("Profile updated.");
        }

        [HttpPost("me/sports")]
        [Authorize]
        public async Task<IActionResult> AddSport(AddSportDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Invalid token.");

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized("Invalid user identifier.");

            var user = await _context.Users
                .Include(u => u.UserSports)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound();

            var sport = await _context.Sports.FindAsync(dto.SportId);
            if (sport == null)
                return NotFound("Sport not found.");

            if (user.UserSports.Any(us => us.SportId == dto.SportId))
                return BadRequest("Sport already assigned to user.");

            user.UserSports.Add(new UserSport
            {
                UserId = user.Id,
                SportId = sport.Id,
                TypicalDistanceKm = dto.TypicalDistanceKm ?? 0
            });

            await _context.SaveChangesAsync();
            return Ok("Sport added to profile.");
        }
        [HttpDelete("me/sports/{sportId}")]
        [Authorize]
        public async Task<IActionResult> RemoveSport(Guid sportId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Invalid token.");

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized("Invalid user identifier.");

            var userSport = await _context.UserSports
                .FirstOrDefaultAsync(us => us.UserId == userId && us.SportId == sportId);

            if (userSport == null)
                return NotFound("Sport not assigned to user.");

            _context.UserSports.Remove(userSport);
            await _context.SaveChangesAsync();

            return Ok("Sport removed from profile.");
        }
        [HttpGet("me/sports")]
        [Authorize]
        public async Task<IActionResult> GetMySports()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Invalid token.");

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized("Invalid user identifier.");

            var user = await _context.Users
                .Include(u => u.UserSports)
                .ThenInclude(us => us.Sport)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound();

            var sports = user.UserSports.Select(us => new
            {
                us.SportId,
                SportName = us.Sport.Name,
                us.TypicalDistanceKm
            });

            return Ok(sports);
        }
        [HttpPatch("me/sports/{sportId}")]
        [Authorize]
        public async Task<IActionResult> UpdateSportDistance(Guid sportId, UpdateSportDistanceDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Invalid token.");

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized("Invalid user identifier.");

            var userSport = await _context.UserSports
                .FirstOrDefaultAsync(us => us.UserId == userId && us.SportId == sportId);

            if (userSport == null)
                return NotFound("Sport not assigned to user.");

            userSport.TypicalDistanceKm = dto.TypicalDistanceKm;
            await _context.SaveChangesAsync();

            return Ok("Sport distance updated.");
        }

    }
}
