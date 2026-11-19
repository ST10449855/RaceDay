using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Attributes;
using RaceDay.API.Data;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets the profile of the currently logged-in user.
        /// </summary>
        [HttpGet("me")]
        [RequireRole("Organiser", "Participant")]
        public async Task<IActionResult> GetMe()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized(new { message = "Not authenticated." });

            var user = await _context.Users
                .Where(u => u.Id == userId.Value)
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Email,
                    Role = u.Role.ToString(),
                    u.ProfilePictureUrl
                })
                .FirstOrDefaultAsync();

            if (user == null) return NotFound(new { message = "User not found." });
            return Ok(user);
        }

        /// <summary>
        /// Updates the profile (name and profile picture URL) of the currently logged-in user.
        /// </summary>
        [HttpPut("me")]
        [RequireRole("Organiser", "Participant")]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileDto dto)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized(new { message = "Not authenticated." });

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null) return NotFound(new { message = "User not found." });

            user.Name = dto.Name;
            user.ProfilePictureUrl = dto.ProfilePictureUrl;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                user.Id,
                user.Name,
                user.Email,
                Role = user.Role.ToString(),
                user.ProfilePictureUrl
            });
        }
    }

    public class UpdateProfileDto
    {
        public string Name { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
    }
}