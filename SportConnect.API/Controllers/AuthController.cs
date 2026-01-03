using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
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
        private readonly IEmailService _emailService;
        private readonly IStringLocalizer<AuthController> _localizer;

        public AuthController(
            AppDbContext context,
            IPasswordHasher<User> hasher,
            IConfiguration config,
            IActionLogger actionLogger,
            IEmailService emailService,
            IStringLocalizer<AuthController> localizer)
        {
            _context = context;
            _hasher = hasher;
            _config = config;
            _actionLogger = actionLogger;
            _emailService = emailService;
            _localizer = localizer;
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

            await _emailService.SendEmailAsync(
            user.Email!,
            "Welcome to SportConnect!",
            $"Hello {user.Name},\n\n" +
            "Thank you for registering with SportConnect. Your account has been successfully created.\n" +
            "You can now log in and start exploring training partners and sports activities.\n\n" +
            "Kind regards,\n" +
            "SportConnect Team"
            );


            await _actionLogger.LogAsync(user.Id, $"user {user.Id} registered with role {role}");

            return Ok(new { message = _localizer["UserRegistered"] });
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return Unauthorized(new { message = _localizer["InvalidCredentials"] });

            if (user.IsBlocked)
                return Unauthorized(new { message = _localizer["UserBlocked"] });

            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (result != PasswordVerificationResult.Success)
                return Unauthorized(new { message = _localizer["InvalidCredentials"] });

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
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User.FindFirstValue(ClaimTypes.Email);
            var roleString = User.FindFirstValue(ClaimTypes.Role);

            var role = Enum.TryParse<UserRole>(roleString, true, out var parsedRole)
                ? parsedRole
                : UserRole.User;

            await _actionLogger.LogAsync(Guid.Parse(userId!), $"user {userId} viewed own profile");

            return Ok(new
            {
                UserId = userId,
                Email = email,
                Role = role.ToString()
            });
        }

        [HttpPost("request-reset")]
        public async Task<IActionResult> RequestPasswordReset(RequestPasswordResetDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return NotFound(new { message = _localizer["UserNotFound"] });

            if (string.IsNullOrWhiteSpace(user.Email))
                return BadRequest(new { message = _localizer["InvalidUserEmail"] });

            var token = Guid.NewGuid().ToString();

            var resetToken = new PasswordResetToken
            {
                UserId = user.Id,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            };

            _context.PasswordResetTokens.Add(resetToken);
            await _context.SaveChangesAsync();

            var resetLink = $"http://localhost:3000/reset-password?token={token}";
            await _emailService.SendEmailAsync(
                user.Email!,
                _localizer["PasswordResetSubject"],
                _localizer["PasswordResetBody", resetLink]);

            return Ok(new { message = _localizer["PasswordResetLinkSent"] });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            var resetToken = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.Token == dto.Token);

            if (resetToken == null || resetToken.ExpiresAt < DateTime.UtcNow)
                return BadRequest(new { message = _localizer["InvalidOrExpiredToken"] });

            var user = await _context.Users.FindAsync(resetToken.UserId);
            if (user == null)
                return NotFound(new { message = _localizer["UserNotFound"] });

            user.PasswordHash = _hasher.HashPassword(user, dto.NewPassword);

            _context.PasswordResetTokens.Remove(resetToken);

            await _context.SaveChangesAsync();

            await _actionLogger.LogAsync(user.Id, $"user {user.Id} reset password");

            return Ok(new { message = _localizer["PasswordResetSuccess"] });
        }
    }
}
