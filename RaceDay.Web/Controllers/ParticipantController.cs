using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace RaceDay.Web.Controllers
{
    public class ParticipantController : Controller
    {
        private readonly ApiClientService _api;

        public ParticipantController(ApiClientService api)
        {
            _api = api;
        }

        private bool IsParticipant(out int userId)
        {
            userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var role = HttpContext.Session.GetString("UserRole");
            return userId > 0 && string.Equals(role, "Participant", StringComparison.OrdinalIgnoreCase);
        }

        private IActionResult RedirectUnauthorised()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Auth", new { returnUrl = Request.Path });

            TempData["ErrorMessage"] = "Access denied: Participant account required.";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enrol(int eventId, int categoryId)
        {
            if (!IsParticipant(out _))
                return RedirectUnauthorised();

            if (categoryId <= 0)
            {
                TempData["ErrorMessage"] = "Please select a valid category before enrolling.";
                return RedirectToAction("Details", "Home", new { id = eventId });
            }

            var (ok, raw, _) = await _api.PostJsonAsync("api/enrolments", new
            {
                EventId = eventId,
                CategoryId = categoryId
            });

            if (!ok)
            {
                var msg = "Could not complete enrolment.";
                try
                {
                    using var doc = JsonDocument.Parse(raw);
                    if (doc.RootElement.TryGetProperty("message", out var m))
                        msg = m.GetString() ?? msg;
                }
                catch { }

                TempData["ErrorMessage"] = msg;
                return RedirectToAction("Details", "Home", new { id = eventId });
            }

            TempData["SuccessMessage"] = "You are officially enrolled in the event!";
            return RedirectToAction(nameof(MyEvents));
        }

        [HttpGet]
        public async Task<IActionResult> MyEvents()
        {
            if (!IsParticipant(out _))
                return RedirectUnauthorised();

            var (ok, raw, _) = await _api.GetRawAsync("api/enrolments/mine");
            var list = ok ? (_api.Deserialize<List<ParticipantEnrolmentItem>>(raw) ?? new()) : new();

            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> MyResults()
        {
            if (!IsParticipant(out _))
                return RedirectUnauthorised();

            var (ok, raw, _) = await _api.GetRawAsync("api/results/mine");
            var list = ok ? (_api.Deserialize<List<ParticipantResultItem>>(raw) ?? new()) : new();

            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            if (!IsParticipant(out _))
                return RedirectUnauthorised();

            var (ok, raw, _) = await _api.GetRawAsync("api/users/me");
            if (!ok)
            {
                TempData["ErrorMessage"] = "Could not load your profile.";
                return RedirectToAction("Index", "Home");
            }

            var profile = _api.Deserialize<UserProfileViewModel>(raw) ?? new UserProfileViewModel();
            return View(profile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(UserProfileViewModel model)
        {
            if (!IsParticipant(out _))
                return RedirectUnauthorised();

            if (!ModelState.IsValid)
                return View(model);

            string? pictureUrl = model.ProfilePictureUrl;
            if (model.ProfilePictureFile != null && model.ProfilePictureFile.Length > 0)
            {
                var upload = await _api.UploadFileAsync(model.ProfilePictureFile, "profiles");
                if (!upload.Success)
                {
                    ModelState.AddModelError(nameof(model.ProfilePictureFile), upload.Message);
                    return View(model);
                }
                pictureUrl = upload.Url;
            }

            var (ok, raw, _) = await _api.PutJsonAsync("api/users/me", new
            {
                model.Name,
                ProfilePictureUrl = pictureUrl
            });

            if (!ok)
            {
                ModelState.AddModelError(string.Empty, $"Failed to update profile: {raw}");
                return View(model);
            }

            model.ProfilePictureUrl = pictureUrl;
            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Profile));
        }
    }

    public class ParticipantEnrolmentItem
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string EventLocation { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public DateTime EnrolledAt { get; set; }
        public string Status { get; set; } = "Confirmed";
    }

    public class ParticipantResultItem
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public TimeSpan FinishTime { get; set; }
        public int Position { get; set; }
        public int TotalFinishers { get; set; }

        public string FormattedRank => $"{FormatOrdinal(Position)} out of {TotalFinishers} finishers";

        private static string FormatOrdinal(int number)
        {
            if (number <= 0) return number.ToString();
            int rem100 = number % 100;
            if (rem100 >= 11 && rem100 <= 13) return $"{number}th";
            return (number % 10) switch
            {
                1 => $"{number}st",
                2 => $"{number}nd",
                3 => $"{number}rd",
                _ => $"{number}th"
            };
        }
    }

    public class UserProfileViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "Participant";
        public string? ProfilePictureUrl { get; set; }
        public IFormFile? ProfilePictureFile { get; set; }
    }
}