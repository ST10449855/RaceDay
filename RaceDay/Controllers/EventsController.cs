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
        /// Retrieves events and their categories with optional filtering.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search = null,
            [FromQuery] EventType? type = null,
            [FromQuery] bool upcomingOnly = false)
        {
            var query = _context.Events
                .Include(e => e.Categories)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(e =>
                    e.Name.ToLower().Contains(term) ||
                    e.Location.ToLower().Contains(term) ||
                    e.Description.ToLower().Contains(term));
            }

            if (type.HasValue)
            {
                query = query.Where(e => e.Type == type.Value);
            }

            if (upcomingOnly)
            {
                query = query.Where(e => e.Date >= DateTime.UtcNow.Date);
            }

            var events = await query.OrderBy(e => e.Date).ToListAsync();
            return Ok(events);
        }

        /// <summary>
        /// Retrieves a single event and its categories by ID.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var evt = await _context.Events
                .Include(e => e.Categories)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (evt == null) return NotFound(new { message = "Event not found." });
            return Ok(evt);
        }

        /// <summary>
        /// Creates a new event (Organisers only).
        /// </summary>
        [HttpPost]
        [RequireRole("Organiser")]
        public async Task<IActionResult> Create([FromBody] EventCreateDto dto)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized(new { message = "Not authenticated." });

            var evt = new Event
            {
                Name = dto.Name,
                Description = dto.Description,
                Date = dto.Date,
                Location = dto.Location,
                DistanceKm = dto.DistanceKm,
                Type = dto.Type,
                BannerUrl = dto.BannerUrl,
                OrganiserId = userId.Value
            };

            _context.Events.Add(evt);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = evt.Id }, evt);
        }

        /// <summary>
        /// Updates an existing event owned by the logged-in Organiser.
        /// </summary>
        [HttpPut("{id}")]
        [RequireRole("Organiser")]
        public async Task<IActionResult> Update(int id, [FromBody] EventCreateDto dto)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized(new { message = "Not authenticated." });

            var existing = await _context.Events.FindAsync(id);
            if (existing == null) return NotFound(new { message = "Event not found." });

            if (existing.OrganiserId != userId.Value)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

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
        /// Deletes an event owned by the logged-in Organiser.
        /// </summary>
        [HttpDelete("{id}")]
        [RequireRole("Organiser")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized(new { message = "Not authenticated." });

            var evt = await _context.Events.FindAsync(id);
            if (evt == null) return NotFound(new { message = "Event not found." });

            if (evt.OrganiserId != userId.Value)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

            _context.Events.Remove(evt);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Adds a category to an event owned by the logged-in Organiser.
        /// </summary>
        [HttpPost("{id}/categories")]
        [RequireRole("Organiser")]
        public async Task<IActionResult> AddCategory(int id, [FromBody] CategoryCreateDto dto)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized(new { message = "Not authenticated." });

            if (dto.MinAge > dto.MaxAge)
                return BadRequest(new { message = "MinAge cannot be greater than MaxAge." });

            var evt = await _context.Events.FindAsync(id);
            if (evt == null) return NotFound(new { message = "Event not found." });

            if (evt.OrganiserId != userId.Value)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

            var category = new Category
            {
                Name = dto.Name,
                MinAge = dto.MinAge,
                MaxAge = dto.MaxAge,
                EventId = id
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return Ok(category);
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

    public class CategoryCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public int MinAge { get; set; }
        public int MaxAge { get; set; }
    }
}