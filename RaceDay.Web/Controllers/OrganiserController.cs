using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Controllers
{
    public class OrganiserController : Controller
    {
        private readonly ApiClientService _api;

        public OrganiserController(ApiClientService api)
        {
            _api = api;
        }

        private bool IsOrganiser(out int userId)
        {
            userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var role = HttpContext.Session.GetString("UserRole");
            return userId > 0 && string.Equals(role, "Organiser", StringComparison.OrdinalIgnoreCase);
        }

        private IActionResult RedirectUnauthorised()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Auth", new { returnUrl = Request.Path });

            TempData["ErrorMessage"] = "Access denied: Organiser privileges required.";
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            if (!IsOrganiser(out var userId))
                return RedirectUnauthorised();

            var (ok, raw, _) = await _api.GetRawAsync("api/events");
            var allEvents = ok ? (_api.Deserialize<List<EventSummaryItem>>(raw) ?? new()) : new();
            var myEvents = allEvents.Where(e => e.OrganiserId == userId).OrderBy(e => e.Date).ToList();

            var rows = new List<OrganiserDashboardRow>();
            foreach (var evt in myEvents)
            {
                var (enrOk, enrRaw, _) = await _api.GetRawAsync($"api/enrolments/event/{evt.Id}");
                var enrolments = enrOk ? (_api.Deserialize<List<EnrolmentSummaryItem>>(enrRaw) ?? new()) : new();

                rows.Add(new OrganiserDashboardRow
                {
                    EventId = evt.Id,
                    Name = evt.Name,
                    Location = evt.Location,
                    Date = evt.Date,
                    DistanceKm = evt.DistanceKm,
                    Type = evt.Type,
                    CategoryCount = evt.Categories?.Count ?? 0,
                    TotalEnrolments = enrolments.Count,
                    IsUpcoming = evt.Date.Date >= DateTime.UtcNow.Date
                });
            }

            var vm = new OrganiserDashboardViewModel
            {
                TotalEvents = rows.Count,
                UpcomingEventsCount = rows.Count(r => r.IsUpcoming),
                TotalEnrolmentsAcrossEvents = rows.Sum(r => r.TotalEnrolments),
                Events = rows
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Events()
        {
            if (!IsOrganiser(out var userId))
                return RedirectUnauthorised();

            var (ok, raw, _) = await _api.GetRawAsync("api/events");
            var allEvents = ok ? (_api.Deserialize<List<EventSummaryItem>>(raw) ?? new()) : new();
            var myEvents = allEvents.Where(e => e.OrganiserId == userId).OrderBy(e => e.Date).ToList();

            return View(myEvents);
        }

        [HttpGet]
        public IActionResult CreateEvent()
        {
            if (!IsOrganiser(out _))
                return RedirectUnauthorised();

            return View(new EventFormViewModel
            {
                Date = DateTime.Today.AddDays(14).AddHours(7)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(EventFormViewModel model)
        {
            if (!IsOrganiser(out _))
                return RedirectUnauthorised();

            if (!ModelState.IsValid)
                return View(model);

            string? bannerUrl = model.BannerUrl;
            if (model.BannerFile != null && model.BannerFile.Length > 0)
            {
                var upload = await _api.UploadFileAsync(model.BannerFile, "banners");
                if (!upload.Success)
                {
                    ModelState.AddModelError(nameof(model.BannerFile), upload.Message);
                    return View(model);
                }
                bannerUrl = upload.Url;
            }

            var payload = new
            {
                model.Name,
                model.Description,
                model.Date,
                model.Location,
                model.DistanceKm,
                model.Type,
                BannerUrl = bannerUrl
            };

            var (ok, raw, _) = await _api.PostJsonAsync("api/events", payload);
            if (!ok)
            {
                ModelState.AddModelError(string.Empty, $"Failed to create event: {raw}");
                return View(model);
            }

            TempData["SuccessMessage"] = "Event created successfully.";
            return RedirectToAction(nameof(Events));
        }

        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            if (!IsOrganiser(out var userId))
                return RedirectUnauthorised();

            var (ok, raw, _) = await _api.GetRawAsync($"api/events/{id}");
            if (!ok)
            {
                TempData["ErrorMessage"] = "Event not found.";
                return RedirectToAction(nameof(Events));
            }

            var evt = _api.Deserialize<EventSummaryItem>(raw);
            if (evt == null) return RedirectToAction(nameof(Events));

            if (evt.OrganiserId != userId)
            {
                TempData["ErrorMessage"] = "You can only edit events you own.";
                return RedirectToAction(nameof(Events));
            }

            return View(new EventFormViewModel
            {
                Id = evt.Id,
                Name = evt.Name,
                Description = evt.Description,
                Date = evt.Date,
                Location = evt.Location,
                DistanceKm = evt.DistanceKm,
                Type = evt.Type,
                BannerUrl = evt.BannerUrl
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(EventFormViewModel model)
        {
            if (!IsOrganiser(out _) || !model.Id.HasValue)
                return RedirectUnauthorised();

            if (!ModelState.IsValid)
                return View(model);

            string? bannerUrl = model.BannerUrl;
            if (model.BannerFile != null && model.BannerFile.Length > 0)
            {
                var upload = await _api.UploadFileAsync(model.BannerFile, "banners");
                if (!upload.Success)
                {
                    ModelState.AddModelError(nameof(model.BannerFile), upload.Message);
                    return View(model);
                }
                bannerUrl = upload.Url;
            }

            var payload = new
            {
                model.Name,
                model.Description,
                model.Date,
                model.Location,
                model.DistanceKm,
                model.Type,
                BannerUrl = bannerUrl
            };

            var (ok, raw, _) = await _api.PutJsonAsync($"api/events/{model.Id.Value}", payload);
            if (!ok)
            {
                ModelState.AddModelError(string.Empty, $"Failed to update event: {raw}");
                return View(model);
            }

            TempData["SuccessMessage"] = "Event updated successfully.";
            return RedirectToAction(nameof(Events));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            if (!IsOrganiser(out _))
                return RedirectUnauthorised();

            var (ok, raw, _) = await _api.DeleteAsync($"api/events/{id}");
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok
                ? "Event deleted successfully."
                : $"Could not delete event: {raw}";

            return RedirectToAction(nameof(Events));
        }

        [HttpGet]
        public async Task<IActionResult> Categories(int id)
        {
            if (!IsOrganiser(out var userId))
                return RedirectUnauthorised();

            var (ok, raw, _) = await _api.GetRawAsync($"api/events/{id}");
            if (!ok)
            {
                TempData["ErrorMessage"] = "Event not found.";
                return RedirectToAction(nameof(Events));
            }

            var evt = _api.Deserialize<EventSummaryItem>(raw);
            if (evt == null || evt.OrganiserId != userId)
            {
                TempData["ErrorMessage"] = "You can only manage categories for your own events.";
                return RedirectToAction(nameof(Events));
            }

            return View(new ManageCategoriesViewModel
            {
                EventId = evt.Id,
                EventName = evt.Name,
                Categories = evt.Categories ?? new(),
                NewCategory = new NewCategoryInput { EventId = evt.Id, MinAge = 18, MaxAge = 99 }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(ManageCategoriesViewModel model)
        {
            if (!IsOrganiser(out _))
                return RedirectUnauthorised();

            var input = model.NewCategory;
            if (string.IsNullOrWhiteSpace(input.Name) || input.MinAge > input.MaxAge)
            {
                TempData["ErrorMessage"] = "Please provide a valid category name and ensure Min Age is not greater than Max Age.";
                return RedirectToAction(nameof(Categories), new { id = input.EventId });
            }

            var (ok, raw, _) = await _api.PostJsonAsync($"api/events/{input.EventId}/categories", new
            {
                input.Name,
                input.MinAge,
                input.MaxAge
            });

            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok
                ? "Category added to event."
                : $"Failed to add category: {raw}";

            return RedirectToAction(nameof(Categories), new { id = input.EventId });
        }

        [HttpGet]
        public async Task<IActionResult> Enrolments(int id)
        {
            if (!IsOrganiser(out _))
                return RedirectUnauthorised();

            var (evOk, evRaw, _) = await _api.GetRawAsync($"api/events/{id}");
            var evt = evOk ? _api.Deserialize<EventSummaryItem>(evRaw) : null;

            var (ok, raw, status) = await _api.GetRawAsync($"api/enrolments/event/{id}");
            if (!ok)
            {
                TempData["ErrorMessage"] = status == System.Net.HttpStatusCode.Forbidden
                    ? "You do not own this event."
                    : "Could not load enrolments for this event.";
                return RedirectToAction(nameof(Events));
            }

            var list = _api.Deserialize<List<EnrolmentSummaryItem>>(raw) ?? new();

            return View(new EventEnrolmentsViewModel
            {
                EventId = id,
                EventName = evt?.Name ?? $"Event #{id}",
                EventDate = evt?.Date,
                Enrolments = list
            });
        }

        [HttpGet]
        public async Task<IActionResult> Results(int id)
        {
            if (!IsOrganiser(out _))
                return RedirectUnauthorised();

            var (evOk, evRaw, _) = await _api.GetRawAsync($"api/events/{id}");
            var evt = evOk ? _api.Deserialize<EventSummaryItem>(evRaw) : null;

            var (ok, raw, status) = await _api.GetRawAsync($"api/enrolments/event/{id}");
            if (!ok)
            {
                TempData["ErrorMessage"] = status == System.Net.HttpStatusCode.Forbidden
                    ? "You do not own this event."
                    : "Could not load enrolled participants for results entry.";
                return RedirectToAction(nameof(Events));
            }

            var list = _api.Deserialize<List<EnrolmentSummaryItem>>(raw) ?? new();

            return View(new EventResultsViewModel
            {
                EventId = id,
                EventName = evt?.Name ?? $"Event #{id}",
                EventDate = evt?.Date,
                Entries = list.Select(e => new ResultEntryRow
                {
                    EnrolmentId = e.Id,
                    ParticipantName = e.ParticipantName,
                    CategoryName = e.CategoryName,
                    FinishTimeText = e.FinishTime?.ToString(@"hh\:mm\:ss") ?? "00:45:00",
                    Position = e.Position ?? 1,
                    AlreadyRecorded = e.HasResult
                }).ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveResult(int eventId, int enrolmentId, string finishTimeText, int position)
        {
            if (!IsOrganiser(out _))
                return RedirectUnauthorised();

            if (!TimeSpan.TryParse(finishTimeText, out var parsedTime))
            {
                TempData["ErrorMessage"] = "Invalid finish time format. Use hh:mm:ss (e.g. 00:42:15).";
                return RedirectToAction(nameof(Results), new { id = eventId });
            }

            if (position < 1)
            {
                TempData["ErrorMessage"] = "Position must be 1 or greater.";
                return RedirectToAction(nameof(Results), new { id = eventId });
            }

            var (ok, raw, _) = await _api.PostJsonAsync("api/results", new
            {
                EnrolmentId = enrolmentId,
                FinishTime = parsedTime,
                Position = position
            });

            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok
                ? "Result saved and published."
                : $"Failed to save result: {raw}";

            return RedirectToAction(nameof(Results), new { id = eventId });
        }
    }

    public class EventSummaryItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Location { get; set; } = string.Empty;
        public double DistanceKm { get; set; }
        public int Type { get; set; }
        public string? BannerUrl { get; set; }
        public int OrganiserId { get; set; }
        public List<CategorySummaryItem> Categories { get; set; } = new();
    }

    public class CategorySummaryItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MinAge { get; set; }
        public int MaxAge { get; set; }
        public int EventId { get; set; }
    }

    public class EnrolmentSummaryItem
    {
        public int Id { get; set; }
        public int ParticipantId { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public string ParticipantEmail { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public DateTime EnrolledAt { get; set; }
        public bool HasResult { get; set; }
        public TimeSpan? FinishTime { get; set; }
        public int? Position { get; set; }
    }

    public class OrganiserDashboardViewModel
    {
        public int TotalEvents { get; set; }
        public int UpcomingEventsCount { get; set; }
        public int TotalEnrolmentsAcrossEvents { get; set; }
        public List<OrganiserDashboardRow> Events { get; set; } = new();
    }

    public class OrganiserDashboardRow
    {
        public int EventId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public double DistanceKm { get; set; }
        public int Type { get; set; }
        public int CategoryCount { get; set; }
        public int TotalEnrolments { get; set; }
        public bool IsUpcoming { get; set; }
    }

    public class EventFormViewModel
    {
        public int? Id { get; set; }

        [Required, StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(600)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public DateTime Date { get; set; }

        [Required, StringLength(150)]
        public string Location { get; set; } = string.Empty;

        [Range(0.1, 500)]
        public double DistanceKm { get; set; }

        [Required]
        public int Type { get; set; } // 0 = Run, 1 = Walk, 2 = Cycle

        public string? BannerUrl { get; set; }
        public IFormFile? BannerFile { get; set; }
    }

    public class ManageCategoriesViewModel
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public List<CategorySummaryItem> Categories { get; set; } = new();
        public NewCategoryInput NewCategory { get; set; } = new();
    }

    public class NewCategoryInput
    {
        public int EventId { get; set; }

        [Required, StringLength(80)]
        public string Name { get; set; } = string.Empty;

        [Range(0, 120)]
        public int MinAge { get; set; }

        [Range(0, 120)]
        public int MaxAge { get; set; }
    }

    public class EventEnrolmentsViewModel
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime? EventDate { get; set; }
        public List<EnrolmentSummaryItem> Enrolments { get; set; } = new();
    }

    public class EventResultsViewModel
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime? EventDate { get; set; }
        public List<ResultEntryRow> Entries { get; set; } = new();
    }

    public class ResultEntryRow
    {
        public int EnrolmentId { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string FinishTimeText { get; set; } = "00:45:00";
        public int Position { get; set; } = 1;
        public bool AlreadyRecorded { get; set; }
    }
}