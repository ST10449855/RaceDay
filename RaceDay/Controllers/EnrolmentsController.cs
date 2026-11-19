using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Attributes;
using RaceDay.API.Data;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EnrolmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EnrolmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Enrols the logged-in participant into an event category (Participants only).
        /// </summary>
        [HttpPost]
        [RequireRole("Participant")]
        public async Task<IActionResult> Enrol([FromBody] EnrolmentDto dto)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized(new { message = "Not authenticated." });

            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId && c.EventId == dto.EventId);
            if (!categoryExists) return BadRequest(new { message = "Invalid category or event selection." });

            var alreadyEnrolled = await _context.Enrolments.AnyAsync(e =>
                e.ParticipantId == userId.Value &&
                e.EventId == dto.EventId &&
                e.CategoryId == dto.CategoryId);

            if (alreadyEnrolled)
                return BadRequest(new { message = "You are already enrolled in this category for this event." });

            var enrolment = new Enrolment
            {
                ParticipantId = userId.Value,
                EventId = dto.EventId,
                CategoryId = dto.CategoryId,
                EnrolledAt = DateTime.UtcNow
            };

            _context.Enrolments.Add(enrolment);
            await _context.SaveChangesAsync();
            return Ok(enrolment);
        }

        /// <summary>
        /// Gets all enrolments for the currently logged-in participant.
        /// </summary>
        [HttpGet("mine")]
        [RequireRole("Participant")]
        public async Task<IActionResult> GetMyEnrolments()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized(new { message = "Not authenticated." });

            var myEnrolments = await (
                from e in _context.Enrolments
                join ev in _context.Events on e.EventId equals ev.Id
                join cat in _context.Categories on e.CategoryId equals cat.Id
                where e.ParticipantId == userId.Value
                orderby ev.Date descending
                select new
                {
                    e.Id,
                    e.EventId,
                    EventName = ev.Name,
                    EventDate = ev.Date,
                    EventLocation = ev.Location,
                    e.CategoryId,
                    CategoryName = cat.Name,
                    e.EnrolledAt,
                    Status = e.Result != null ? "Completed" : "Confirmed"
                }
            ).ToListAsync();

            return Ok(myEnrolments);
        }

        /// <summary>
        /// Views all enrolments for a specific event owned by the logged-in Organiser.
        /// </summary>
        [HttpGet("event/{eventId}")]
        [RequireRole("Organiser")]
        public async Task<IActionResult> GetByEvent(int eventId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized(new { message = "Not authenticated." });

            var evt = await _context.Events.FindAsync(eventId);
            if (evt == null) return NotFound(new { message = "Event not found." });

            if (evt.OrganiserId != userId.Value)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

            var enrolments = await (
                from e in _context.Enrolments
                join p in _context.Users on e.ParticipantId equals p.Id
                join cat in _context.Categories on e.CategoryId equals cat.Id
                where e.EventId == eventId
                select new
                {
                    e.Id,
                    e.ParticipantId,
                    ParticipantName = p.Name,
                    ParticipantEmail = p.Email,
                    e.CategoryId,
                    CategoryName = cat.Name,
                    e.EnrolledAt,
                    HasResult = e.Result != null,
                    FinishTime = e.Result != null ? (TimeSpan?)e.Result.FinishTime : null,
                    Position = e.Result != null ? (int?)e.Result.Position : null
                }
            ).ToListAsync();

            return Ok(enrolments);
        }
    }

    public class EnrolmentDto
    {
        public int EventId { get; set; }
        public int CategoryId { get; set; }
    }
}