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
        /// Captures or updates a participant's finish time and position (Organisers only).
        /// </summary>
        [HttpPost]
        [RequireRole("Organiser")]
        public async Task<IActionResult> CaptureResult([FromBody] ResultDto dto)
        {
            var enrolment = await _context.Enrolments.Include(e => e.Result).FirstOrDefaultAsync(e => e.Id == dto.EnrolmentId);
            if (enrolment == null) return NotFound(new { message = "Enrolment record not found." });

            if (enrolment.Result != null)
            {
                // Update existing result (0..1 relationship)
                enrolment.Result.FinishTime = dto.FinishTime;
                enrolment.Result.Position = dto.Position;
            }
            else
            {
                // Create new result
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
        /// Views results (Accessible by all roles).
        /// </summary>
        [HttpGet]
        [RequireRole("Organiser", "Participant")]
        public async Task<IActionResult> GetResults()
        {
            var results = await _context.Results
                .Include(r => r.Enrolment)
                .ThenInclude(e => e!.Participant) // Add the dot here!
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