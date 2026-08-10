using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace FrontendUI.Controllers
{
    public class AuthController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string _gatewayUrl = Environment.GetEnvironmentVariable("GATEWAY_URL")
            ?? "http://localhost:5134";

        public AuthController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        // GET /Auth/Register
        public IActionResult Register() => View();

        // POST /Auth/Register
        [HttpPost]
        public async Task<IActionResult> Register(string username,
            string email, string password)
        {
            if (string.IsNullOrEmpty(username) ||
                string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "All fields are required";
                return View();
            }

            var body = JsonSerializer.Serialize(new { username, email, password });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(
                $"{_gatewayUrl}/auth/register", content);

            if (response.IsSuccessStatusCode)
                return RedirectToAction("Login");

            var errorContent = await response.Content.ReadAsStringAsync();

            if (errorContent.Contains("DuplicateUserName"))
                ViewBag.Error = "This username is already taken. Please choose a different one.";
            else if (errorContent.Contains("DuplicateEmail"))
                ViewBag.Error = "This email is already registered. Please login instead.";
            else if (errorContent.Contains("PasswordTooShort"))
                ViewBag.Error = "Password too short. Minimum 6 characters required.";
            else
                ViewBag.Error = "Registration failed. Please try again.";

            return View();
        }

        // GET /Auth/Login
        public IActionResult Login() => View();

        // POST /Auth/Login
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "All fields are required";
                return View();
            }

            var body = JsonSerializer.Serialize(new { email, password });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(
                $"{_gatewayUrl}/auth/login", content);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<JsonElement>(json);
                var token = data.GetProperty("token").GetString();
                var role = GetRoleFromToken(token!);

                // Generate consistent integer ID from email
                var userId = Math.Abs(email.GetHashCode() % 1000000).ToString();

                HttpContext.Session.SetString("JWTToken", token!);
                HttpContext.Session.SetString("UserRole", role);
                HttpContext.Session.SetString("UserEmail", email);
                HttpContext.Session.SetString("UserId", userId);

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Invalid email or password";
            return View();
        }

        // GET /Auth/CheckUsername
        [HttpGet]
        public async Task<IActionResult> CheckUsername(string username)
        {
            var response = await _httpClient.GetAsync(
                $"{_gatewayUrl}/auth/checkusername?username={Uri.EscapeDataString(username)}");
            if (!response.IsSuccessStatusCode)
                return Json(new { taken = false });
            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(json);
            return Json(new { taken = data.GetProperty("taken").GetBoolean() });
        }

        [HttpGet]
        public async Task<IActionResult> CheckEmail(string email)
        {
            var response = await _httpClient.GetAsync(
                $"{_gatewayUrl}/auth/checkemail?email={Uri.EscapeDataString(email)}");
            if (!response.IsSuccessStatusCode)
                return Json(new { taken = false });
            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(json);
            return Json(new { taken = data.GetProperty("taken").GetBoolean() });
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        private void SetAuthHeaderForCheck()
        {
            var token = HttpContext.Session.GetString("JWTToken");
            if (!string.IsNullOrEmpty(token))
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Bearer", token);
        }
        private string GetUserIdFromToken(string token)
        {
            try
            {
                var parts = token.Split('.');
                var payload = parts[1];
                var padded = payload.PadRight(
                    payload.Length + (4 - payload.Length % 4) % 4, '=');
                var decoded = Encoding.UTF8.GetString(
                    Convert.FromBase64String(padded));
                var json = JsonSerializer.Deserialize<JsonElement>(decoded);

                foreach (var prop in json.EnumerateObject())
                {
                    if (prop.Name.Contains("nameidentifier",
                        StringComparison.OrdinalIgnoreCase))
                        return prop.Value.GetString() ?? "1";
                }
            }
            catch { }
            return "1";
        }

        private string GetRoleFromToken(string token)
        {
            try
            {
                var parts = token.Split('.');
                var payload = parts[1];
                var padded = payload.PadRight(
                    payload.Length + (4 - payload.Length % 4) % 4, '=');
                var decoded = Encoding.UTF8.GetString(
                    Convert.FromBase64String(padded));
                var json = JsonSerializer.Deserialize<JsonElement>(decoded);

                foreach (var prop in json.EnumerateObject())
                {
                    if (prop.Name.Contains("role",
                        StringComparison.OrdinalIgnoreCase))
                        return prop.Value.GetString() ?? "Customer";
                }
            }
            catch { }
            return "Customer";
        }
    }
}