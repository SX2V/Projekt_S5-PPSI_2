using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportConnect.API.Data;
using SportConnect.API.Models;

namespace SportConnect.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SportsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Sport>>> GetAllSports()
        {
            return await _context.Sports.ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<Sport>> CreateSport(Sport sport)
        {
            _context.Sports.Add(sport);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetAllSports), new { id = sport.Id }, sport);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSport(Guid id)
        {
            var sport = await _context.Sports.FindAsync(id);
            if (sport == null)
            {
                return NotFound();
            }

            _context.Sports.Remove(sport);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

}
