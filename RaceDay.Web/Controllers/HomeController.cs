using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApiClientService _api;

        public HomeController(ApiClientService api)
        {
            _api = api;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search = null, int? type = null, bool showPast = false)
        {
            var queryParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(search))
                queryParts.Add($"search={Uri.EscapeDataString(search)}");
            if (type.HasValue)
                queryParts.Add($"type={type.Value}");
            if (!showPast)
                queryParts.Add("upcomingOnly=true");

            var url = queryParts.Count > 0
                ? $"api/events?{string.Join("&", queryParts)}"
                : "api/events";

            var (ok, raw, _) = await _api.GetRawAsync(url);
            var events = ok ? (_api.Deserialize<List<EventSummaryItem>>(raw) ?? new()) : new();

            return View(new EventBrowseViewModel
            {
                Search = search,
                SelectedType = type,
                ShowPast = showPast,
                Events = events
            });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var (ok, raw, _) = await _api.GetRawAsync($"api/events/{id}");
            if (!ok)
            {
                TempData["ErrorMessage"] = "Event not found.";
                return RedirectToAction(nameof(Index));
            }

            var evt = _api.Deserialize<EventSummaryItem>(raw);
            if (evt == null)
            {
                TempData["ErrorMessage"] = "Unable to load event details.";
                return RedirectToAction(nameof(Index));
            }

            return View(evt);
        }
    }

    public class EventBrowseViewModel
    {
        public string? Search { get; set; }
        public int? SelectedType { get; set; }
        public bool ShowPast { get; set; }
        public List<EventSummaryItem> Events { get; set; } = new();
    }
}