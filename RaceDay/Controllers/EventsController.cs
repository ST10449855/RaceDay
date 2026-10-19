using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Attributes;
using RaceDay.API.Data;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EventsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves all events and their categories (Accessible by Organisers and Participants).
        /// </summary>
        [HttpGet]
        [RequireRole("Organiser", "Participant")]
        public async Task<IActionResult> GetAll()
        {
            var events = await _context.Events
                .Include(e => e.Categories)
                .ToListAsync();
            return Ok(events);
        }

        /// <summary>
        /// Creates a new event (Organisers only).
        /// </summary>
        [HttpPost]
        [RequireRole("Organiser")]
        public async Task<IActionResult> Create([FromBody] EventCreateDto dto)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            var evt = new Event
            {
                Name = dto.Name,
                Description = dto.Description,
                Date = dto.Date,
                Location = dto.Location,
                DistanceKm = dto.DistanceKm,
                Type = dto.Type,
                BannerUrl = dto.BannerUrl,
                OrganiserId = userId!.Value
            };

            _context.Events.Add(evt);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetAll), new { id = evt.Id }, evt);
        }

        /// <summary>
        /// Updates an existing event (Organisers only).
        /// </summary>
        [HttpPut("{id}")]
        [RequireRole("Organiser")]
        public async Task<IActionResult> Update(int id, [FromBody] EventCreateDto dto)
        {
            var existing = await _context.Events.FindAsync(id);
            if (existing == null) return NotFound(new { message = "Event not found." });

            existing.Name = dto.Name;
            existing.Description = dto.Description;
            existing.Date = dto.Date;
            existing.Location = dto.Location;
            existing.DistanceKm = dto.DistanceKm;
            existing.Type = dto.Type;
            existing.BannerUrl = dto.BannerUrl;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        /// <summary>
        /// Deletes an event (Organisers only).
        /// </summary>
        [HttpDelete("{id}")]
        [RequireRole("Organiser")]
        public async Task<IActionResult> Delete(int id)
        {
            var evt = await _context.Events.FindAsync(id);
            if (evt == null) return NotFound(new { message = "Event not found." });

            _context.Events.Remove(evt);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    public class EventCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Location { get; set; } = string.Empty;
        public double DistanceKm { get; set; }
        public EventType Type { get; set; }
        public string? BannerUrl { get; set; }
    }
}