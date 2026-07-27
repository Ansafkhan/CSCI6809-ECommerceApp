using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace FrontendUI.Controllers
{
    public class ProductsController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string _gatewayUrl = Environment.GetEnvironmentVariable("GATEWAY_URL") ?? "http://localhost:5134";

        public ProductsController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        private void SetAuthHeader()
        {
            var token = HttpContext.Session.GetString("JWTToken");
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Bearer", token);
            }
        }

        // GET /Products
        public async Task<IActionResult> Index()
        {
            var token = HttpContext.Session.GetString("JWTToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            SetAuthHeader();

            try
            {
                var response = await _httpClient.GetAsync($"{_gatewayUrl}/products");

                if (!response.IsSuccessStatusCode)
                {
                    // Token expired or invalid - redirect to login
                    HttpContext.Session.Clear();
                    return RedirectToAction("Login", "Auth");
                }

                var json = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(json) || json == "null")
                {
                    ViewBag.Role = HttpContext.Session.GetString("UserRole");
                    return View(new List<System.Text.Json.JsonElement>());
                }

                var products = System.Text.Json.JsonSerializer.Deserialize<List<System.Text.Json.JsonElement>>(json,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                ViewBag.Role = HttpContext.Session.GetString("UserRole");
                return View(products ?? new List<System.Text.Json.JsonElement>());
            }
            catch
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Auth");
            }
        }

        // GET /Products/Create
        public IActionResult Create()
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
                return RedirectToAction("Index");
            return View();
        }

        // POST /Products/Create
        [HttpPost]
        public async Task<IActionResult> Create(string name,
     string description, decimal price, int stock, string category)
        {
            if (string.IsNullOrEmpty(name) || price <= 0 || stock < 0)
            {
                ViewBag.Error = "Please fill all fields correctly";
                return View();
            }

            SetAuthHeader();
            var body = JsonSerializer.Serialize(
                new { name, description, price, stock, category = category ?? "General" });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync($"{_gatewayUrl}/products", content);
            return RedirectToAction("Index");
        }
        [HttpPost]
        public async Task<IActionResult> Create(string name, string description,
    decimal price, int stock, string category, IFormFile? imageFile)
        {
            if (string.IsNullOrEmpty(name) || price <= 0 || stock < 0)
            {
                ViewBag.Error = "Please fill all fields correctly";
                return View();
            }

            // Handle image upload
            if (imageFile != null && imageFile.Length > 0)
            {
                var ext = Path.GetExtension(imageFile.FileName);
                var fileName = name.ToLower().Replace(" ", "") + ext;
                var path = Path.Combine(Directory.GetCurrentDirectory(),
                    "wwwroot", "images", fileName);
                using var stream = new FileStream(path, FileMode.Create);
                await imageFile.CopyToAsync(stream);
            }

            SetAuthHeader();
            var body = JsonSerializer.Serialize(
                new
                {
                    name,
                    description,
                    price,
                    stock,
                    category = category ?? "General"
                });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync($"{_gatewayUrl}/products", content);
            return RedirectToAction("Index");
        }

        // POST /Products/Delete
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            SetAuthHeader();
            await _httpClient.DeleteAsync($"{_gatewayUrl}/products/{id}");
            return RedirectToAction("Index");
        }
    }
}