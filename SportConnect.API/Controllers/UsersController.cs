using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportConnect.API.Data;
using SportConnect.API.Models;
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

            // Aktualizacja pól
            existingUser.Name = updatedUser.Name;
            existingUser.Email = updatedUser.Email;
            existingUser.IsAvailableNow = updatedUser.IsAvailableNow;
            existingUser.SearchRadiusKm = updatedUser.SearchRadiusKm;

            await _context.SaveChangesAsync();

            return Ok(existingUser);
        }

        [HttpPatch("{id}")]
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

    }
}
