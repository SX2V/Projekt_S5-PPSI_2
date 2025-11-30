using System;
using System.Threading.Tasks;
using SportConnect.API.Data;
using SportConnect.API.Models;

namespace SportConnect.API.Services
{
    public interface IActionLogger
    {
        Task LogAsync(Guid userId, string action);
    }

    public class ActionLogger : IActionLogger
    {
        private readonly AppDbContext _context;

        public ActionLogger(AppDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(Guid userId, string action)
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
    }
}
