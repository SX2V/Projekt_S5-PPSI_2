using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SportConnect.API.Data;
using SportConnect.API.Dtos;
using SportConnect.API.Models;
using SportConnect.API.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SportConnect.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<User> _hasher;
        private readonly IConfiguration _config;
        private readonly IActionLogger _actionLogger;

        public AuthController(AppDbContext context, IPasswordHasher<User> hasher, IConfiguration config, IActionLogger actionLogger)
        {
            _context = context;
            _hasher = hasher;
            _config = config;
            _actionLogger = actionLogger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var role = Enum.TryParse<UserRole>(dto.Role, true, out var parsedRole)
                ? parsedRole
                : UserRole.User;

            var user = new User
            {
                Email = dto.Email,
                Name = dto.Name,
                Role = role
            };

            user.PasswordHash = _hasher.HashPassword(user, dto.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await _actionLogger.LogAsync(user.Id, $"user {user.Id} registered with role {role}");

            return Ok("User registered.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return Unauthorized("Invalid credentials.");

            if (user.IsBlocked)
                return Unauthorized("User is blocked.");

            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (result != PasswordVerificationResult.Success)
                return Unauthorized("Invalid credentials.");

            var token = GenerateJwtToken(user);

            await _actionLogger.LogAsync(user.Id, $"user {user.Id} logged in");

            return Ok(new { token });
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpGet("me")]
        [Authorize]
        public IActionResult GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User.FindFirstValue(ClaimTypes.Email);
            var roleString = User.FindFirstValue(ClaimTypes.Role);

            var role = Enum.TryParse<UserRole>(roleString, true, out var parsedRole)
                ? parsedRole
                : UserRole.User;

            _actionLogger.LogAsync(Guid.Parse(userId!), $"user {userId} viewed own profile");

            return Ok(new
            {
                UserId = userId,
                Email = email,
                Role = role.ToString()
            });
        }
    }
}
