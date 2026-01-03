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
        private readonly IEmailService _emailService;
        // Serwisy dla logowania społecznościowego
        private readonly IFacebookAuthService _facebookAuthService;
        private readonly IStravaAuthService _stravaAuthService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            AppDbContext context,
            IPasswordHasher<User> hasher,
            IConfiguration config,
            IActionLogger actionLogger,
            IEmailService emailService,
            // Dependency Injection dla nowych serwisów
            IFacebookAuthService facebookAuthService,
            IStravaAuthService stravaAuthService,
            ILogger<AuthController> logger)
        {
            _context = context;
            _hasher = hasher;
            _config = config;
            _actionLogger = actionLogger;
            _emailService = emailService;
            _facebookAuthService = facebookAuthService;
            _stravaAuthService = stravaAuthService;
            _logger = logger;
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

        // Logowanie przez Facebook - przyjmuje access token z Facebook SDK
        
        [HttpPost("facebook")]
        public async Task<IActionResult> FacebookLogin([FromBody] FacebookLoginDto dto)
        {
            try
            {
                // 1. Walidacja tokenu Facebook
                var facebookUser = await _facebookAuthService.ValidateAccessTokenAsync(dto.AccessToken);
                if (facebookUser == null)
                {
                    _logger.LogWarning("Invalid Facebook token received");
                    return Unauthorized(new { message = "Invalid Facebook token" });
                }

                // 2. Sprawdź czy użytkownik już istnieje (po FacebookId lub Email)
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.FacebookId == facebookUser.Id);

                if (user == null && !string.IsNullOrEmpty(facebookUser.Email))
                {
                    user = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == facebookUser.Email);
                }

                // 3. Jeśli użytkownik nie istnieje - utwórz nowego
                if (user == null)
                {
                    user = new User
                    {
                        Email = facebookUser.Email,
                        Name = facebookUser.Name,
                        FacebookId = facebookUser.Id,
                        Role = UserRole.User,
                        IsBlocked = false,
                        PasswordHash = string.Empty // Nie używamy hasła dla social login
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("New user created via Facebook: {Email}", user.Email);
                    await _actionLogger.LogAsync(user.Id, $"user {user.Id} registered via Facebook");
                }
                else if (string.IsNullOrEmpty(user.FacebookId))
                {
                    // Jeśli użytkownik istniał ale nie miał FacebookId - zaktualizuj
                    user.FacebookId = facebookUser.Id;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("FacebookId linked to existing user: {Email}", user.Email);
                }

                // 4. Sprawdź czy użytkownik nie jest zablokowany
                if (user.IsBlocked)
                {
                    return Unauthorized(new { message = "User is blocked" });
                }

                // 5. Wygeneruj JWT
                var token = GenerateJwtToken(user);

                await _actionLogger.LogAsync(user.Id, $"user {user.Id} logged in via Facebook");

                return Ok(new
                {
                    token,
                    user = new
                    {
                        id = user.Id,
                        email = user.Email,
                        name = user.Name,
                        role = user.Role.ToString()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Facebook login");
                return StatusCode(500, new { message = "Internal server error during Facebook login" });
            }
        }

        
        // Logowanie przez Strava - przyjmuje authorization code z OAuth flow

        [HttpPost("strava")]
        public async Task<IActionResult> StravaLogin([FromBody] StravaLoginDto dto)
        {
            try
            {
                // 1. Wymień authorization code na access token
                var tokenResponse = await _stravaAuthService.ExchangeCodeForTokenAsync(dto.Code);
                if (tokenResponse == null)
                {
                    _logger.LogWarning("Invalid Strava authorization code");
                    return Unauthorized(new { message = "Invalid Strava authorization code" });
                }

                // 2. Pobierz profil użytkownika ze Strava
                var stravaUser = await _stravaAuthService.GetAthleteProfileAsync(tokenResponse.AccessToken);
                if (stravaUser == null)
                {
                    _logger.LogWarning("Could not retrieve Strava profile");
                    return Unauthorized(new { message = "Could not retrieve Strava profile" });
                }

                // 3. Sprawdź czy użytkownik już istnieje (po StravaId lub Email)
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.StravaId == stravaUser.Id);

                if (user == null && !string.IsNullOrEmpty(stravaUser.Email))
                {
                    user = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == stravaUser.Email);
                }

                // 4. Jeśli użytkownik nie istnieje - utwórz nowego
                if (user == null)
                {
                    // Strava może nie zwracać emaila - generujemy fallback
                    var email = stravaUser.Email ?? $"strava_{stravaUser.Id}@sportconnect.local";
                    var name = $"{stravaUser.Firstname} {stravaUser.Lastname}".Trim();

                    user = new User
                    {
                        Email = email,
                        Name = string.IsNullOrEmpty(name) ? stravaUser.Username : name,
                        StravaId = stravaUser.Id,
                        Role = UserRole.User,
                        IsBlocked = false,
                        PasswordHash = string.Empty // Nie używamy hasła dla social login
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("New user created via Strava: {Email}", user.Email);
                    await _actionLogger.LogAsync(user.Id, $"user {user.Id} registered via Strava");
                }
                else if (user.StravaId == null)
                {
                    // Jeśli użytkownik istniał ale nie miał StravaId - zaktualizuj
                    user.StravaId = stravaUser.Id;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("StravaId linked to existing user: {Email}", user.Email);
                }

                // 5. Sprawdź czy użytkownik nie jest zablokowany
                if (user.IsBlocked)
                {
                    return Unauthorized(new { message = "User is blocked" });
                }

                // 6. Wygeneruj JWT
                var token = GenerateJwtToken(user);

                await _actionLogger.LogAsync(user.Id, $"user {user.Id} logged in via Strava");

                return Ok(new
                {
                    token,
                    user = new
                    {
                        id = user.Id,
                        email = user.Email,
                        name = user.Name,
                        role = user.Role.ToString()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Strava login");
                return StatusCode(500, new { message = "Internal server error during Strava login" });
            }
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
                return NotFound("User not found.");

            if (string.IsNullOrWhiteSpace(user.Email))
                return BadRequest("User email is not valid.");

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
                "Password Reset",
                $"Kliknij link aby zresetować hasło: {resetLink}");

            return Ok("Password reset link sent.");
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            var resetToken = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.Token == dto.Token);

            if (resetToken == null || resetToken.ExpiresAt < DateTime.UtcNow)
                return BadRequest("Invalid or expired token.");

            var user = await _context.Users.FindAsync(resetToken.UserId);
            if (user == null)
                return NotFound("User not found.");

            user.PasswordHash = _hasher.HashPassword(user, dto.NewPassword);

            _context.PasswordResetTokens.Remove(resetToken);

            await _context.SaveChangesAsync();

            await _actionLogger.LogAsync(user.Id, $"user {user.Id} reset password");

            return Ok("Password has been reset successfully.");
        }
    }
}
