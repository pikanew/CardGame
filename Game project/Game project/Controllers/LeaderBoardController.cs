using Game_project.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Game_project.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeaderboardController : ControllerBase
    {
        private readonly GameDbContext _db;

        public LeaderboardController(GameDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetLeaderboard()
        {
            var leaderboard = await _db.PlayerStats
                .Include(s => s.User)
                .OrderByDescending(s => s.Score)
                .Take(100)
                .Select(s => new
                {
                    s.User.Username,
                    s.Score,
                    s.Wins,
                    s.Losses,
                    s.Draws
                })
                .ToListAsync();

            return Ok(leaderboard);
        }
    }
}
    

