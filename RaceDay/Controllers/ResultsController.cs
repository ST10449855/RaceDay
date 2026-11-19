using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Attributes;
using RaceDay.API.Data;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResultsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ResultsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Captures or updates a participant's finish time and position for an owned event (Organisers only).
        /// </summary>
        [HttpPost]
        [RequireRole("Organiser")]
        public async Task<IActionResult> CaptureResult([FromBody] ResultDto dto)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized(new { message = "Not authenticated." });

            var enrolment = await _context.Enrolments
                .Include(e => e.Result)
                .FirstOrDefaultAsync(e => e.Id == dto.EnrolmentId);

            if (enrolment == null) return NotFound(new { message = "Enrolment record not found." });

            var evt = await _context.Events.FindAsync(enrolment.EventId);
            if (evt == null) return NotFound(new { message = "Associated event not found." });

            if (evt.OrganiserId != userId.Value)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

            if (enrolment.Result != null)
            {
                enrolment.Result.FinishTime = dto.FinishTime;
                enrolment.Result.Position = dto.Position;
            }
            else
            {
                var result = new Result
                {
                    EnrolmentId = dto.EnrolmentId,
                    FinishTime = dto.FinishTime,
                    Position = dto.Position
                };
                _context.Results.Add(result);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Result captured successfully." });
        }

        /// <summary>
        /// Gets results for the currently logged-in participant, including total finishers per event.
        /// </summary>
        [HttpGet("mine")]
        [RequireRole("Participant")]
        public async Task<IActionResult> GetMyResults()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized(new { message = "Not authenticated." });

            var myRows = await (
                from r in _context.Results
                join e in _context.Enrolments on r.EnrolmentId equals e.Id
                join ev in _context.Events on e.EventId equals ev.Id
                join cat in _context.Categories on e.CategoryId equals cat.Id
                where e.ParticipantId == userId.Value
                orderby ev.Date descending
                select new
                {
                    r.Id,
                    e.EventId,
                    EventName = ev.Name,
                    EventDate = ev.Date,
                    CategoryName = cat.Name,
                    r.FinishTime,
                    r.Position,
                    TotalFinishers = _context.Results.Count(x => x.Enrolment != null && x.Enrolment.EventId == e.EventId)
                }
            ).ToListAsync();

            return Ok(myRows);
        }

        /// <summary>
        /// Views results (Accessible by all roles).
        /// </summary>
        [HttpGet]
        [RequireRole("Organiser", "Participant")]
        public async Task<IActionResult> GetResults()
        {
            var results = await _context.Results
                .Include(r => r.Enrolment)
                .ThenInclude(e => e!.Participant)
                .ToListAsync();
            return Ok(results);
        }
    }

    public class ResultDto
    {
        public int EnrolmentId { get; set; }
        public TimeSpan FinishTime { get; set; }
        public int Position { get; set; }
    }
}