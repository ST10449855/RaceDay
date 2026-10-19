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
            var userId = HttpContext.Session.GetInt32("UserId")!.Value;

            // Verify category exists
            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId && c.EventId == dto.EventId);
            if (!categoryExists) return BadRequest(new { message = "Invalid category or event selection." });

            var enrolment = new Enrolment
            {
                ParticipantId = userId,
                EventId = dto.EventId,
                CategoryId = dto.CategoryId,
                EnrolledAt = DateTime.UtcNow
            };

            _context.Enrolments.Add(enrolment);
            await _context.SaveChangesAsync();
            return Ok(enrolment);
        }

        /// <summary>
        /// Views all enrolments for a specific event (Organisers only).
        /// </summary>
        [HttpGet("event/{eventId}")]
        [RequireRole("Organiser")]
        public async Task<IActionResult> GetByEvent(int eventId)
        {
            var enrolments = await _context.Enrolments
                .Where(e => e.EventId == eventId)
                .Include(e => e.Participant)
                .Include(e => e.Result)
                .ToListAsync();

            return Ok(enrolments);
        }
    }

    public class EnrolmentDto
    {
        public int EventId { get; set; }
        public int CategoryId { get; set; }
    }
}