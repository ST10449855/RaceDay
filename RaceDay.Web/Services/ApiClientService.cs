using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RaceDay.Web.Services
{
    public class ApiClientService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ApiClientService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
        }

        private HttpContext? HttpContext => _httpContextAccessor.HttpContext;

        private string? GetStoredApiCookie()
        {
            return HttpContext?.Session.GetString("ApiSessionCookie");
        }

        private void StoreApiCookie(HttpResponseMessage response)
        {
            if (response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders))
            {
                var cookies = new List<string>();
                foreach (var header in setCookieHeaders)
                {
                    var cookiePart = header.Split(';')[0].Trim();
                    if (!string.IsNullOrEmpty(cookiePart))
                        cookies.Add(cookiePart);
                }

                if (cookies.Count > 0)
                {
                    HttpContext?.Session.SetString("ApiSessionCookie", string.Join("; ", cookies));
                }
            }
        }

        private HttpRequestMessage CreateRequest(HttpMethod method, string relativeUrl, object? body = null)
        {
            var request = new HttpRequestMessage(method, relativeUrl);

            var cookie = GetStoredApiCookie();
            if (!string.IsNullOrWhiteSpace(cookie))
            {
                request.Headers.Add("Cookie", cookie);
            }

            if (body != null)
            {
                var json = JsonSerializer.Serialize(body);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            return request;
        }

        public async Task<(bool Success, string Message)> RegisterAsync(string name, string email, string password, int role, string? profilePictureUrl = null)
        {
            var payload = new
            {
                Name = name,
                Email = email,
                Password = password,
                Role = role,
                ProfilePictureUrl = profilePictureUrl
            };

            var req = CreateRequest(HttpMethod.Post, "api/auth/register", payload);
            var res = await _httpClient.SendAsync(req);
            var raw = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                return (false, ExtractErrorMessage(raw, "Registration failed."));

            return (true, "Registration successful. Please log in.");
        }

        public async Task<(bool Success, string Message, int? UserId, string? Role)> LoginAsync(string email, string password)
        {
            var payload = new { Email = email, Password = password };
            var req = CreateRequest(HttpMethod.Post, "api/auth/login", payload);
            var res = await _httpClient.SendAsync(req);
            var raw = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                return (false, ExtractErrorMessage(raw, "Invalid email or password."), null, null);

            StoreApiCookie(res);

            int? userId = null;
            string? role = null;

            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.TryGetProperty("userId", out var uidProp) && uidProp.TryGetInt32(out var idVal))
                    userId = idVal;
                if (doc.RootElement.TryGetProperty("role", out var roleProp))
                    role = roleProp.GetString();
            }
            catch { }

            if (HttpContext != null)
            {
                if (userId.HasValue) HttpContext.Session.SetInt32("UserId", userId.Value);
                if (!string.IsNullOrEmpty(role)) HttpContext.Session.SetString("UserRole", role);
                HttpContext.Session.SetString("UserEmail", email);
            }

            return (true, "Logged in successfully.", userId, role);
        }

        public async Task LogoutAsync()
        {
            try
            {
                var req = CreateRequest(HttpMethod.Post, "api/auth/logout");
                await _httpClient.SendAsync(req);
            }
            catch { }

            HttpContext?.Session.Clear();
        }

        public async Task<(bool Success, string RawJson, HttpStatusCode StatusCode)> GetRawAsync(string relativeUrl)
        {
            var req = CreateRequest(HttpMethod.Get, relativeUrl);
            var res = await _httpClient.SendAsync(req);
            var raw = await res.Content.ReadAsStringAsync();
            return (res.IsSuccessStatusCode, raw, res.StatusCode);
        }

        public async Task<(bool Success, string RawJson, HttpStatusCode StatusCode)> PostJsonAsync(string relativeUrl, object body)
        {
            var req = CreateRequest(HttpMethod.Post, relativeUrl, body);
            var res = await _httpClient.SendAsync(req);
            var raw = await res.Content.ReadAsStringAsync();
            return (res.IsSuccessStatusCode, raw, res.StatusCode);
        }

        public async Task<(bool Success, string RawJson, HttpStatusCode StatusCode)> PutJsonAsync(string relativeUrl, object body)
        {
            var req = CreateRequest(HttpMethod.Put, relativeUrl, body);
            var res = await _httpClient.SendAsync(req);
            var raw = await res.Content.ReadAsStringAsync();
            return (res.IsSuccessStatusCode, raw, res.StatusCode);
        }

        public async Task<(bool Success, string RawJson, HttpStatusCode StatusCode)> DeleteAsync(string relativeUrl)
        {
            var req = CreateRequest(HttpMethod.Delete, relativeUrl);
            var res = await _httpClient.SendAsync(req);
            var raw = await res.Content.ReadAsStringAsync();
            return (res.IsSuccessStatusCode, raw, res.StatusCode);
        }

        public async Task<(bool Success, string? Url, string Message)> UploadFileAsync(IFormFile file, string folder)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, $"api/files/upload?folder={Uri.EscapeDataString(folder)}");
            var cookie = GetStoredApiCookie();
            if (!string.IsNullOrWhiteSpace(cookie))
            {
                req.Headers.Add("Cookie", cookie);
            }

            using var content = new MultipartFormDataContent();
            await using var stream = file.OpenReadStream();
            var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            content.Add(streamContent, "file", file.FileName);
            req.Content = content;

            var res = await _httpClient.SendAsync(req);
            var raw = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                return (false, null, ExtractErrorMessage(raw, "File upload failed."));

            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.TryGetProperty("url", out var urlProp))
                {
                    return (true, urlProp.GetString(), "Uploaded successfully.");
                }
            }
            catch { }

            return (false, null, "Could not parse uploaded image URL.");
        }

        public T? Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }

        private static string ExtractErrorMessage(string raw, string fallback)
        {
            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.TryGetProperty("message", out var msg))
                    return msg.GetString() ?? fallback;
            }
            catch { }

            return string.IsNullOrWhiteSpace(raw) ? fallback : raw;
        }
    }
}