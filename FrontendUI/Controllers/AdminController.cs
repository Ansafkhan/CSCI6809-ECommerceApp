using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace FrontendUI.Controllers
{
    public class AdminController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string _gatewayUrl;

        public AdminController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _gatewayUrl = Environment.GetEnvironmentVariable("GATEWAY_URL")
                ?? "http://localhost:5134";
        }

        private void SetAuthHeader()
        {
            var token = HttpContext.Session.GetString("JWTToken");
            if (!string.IsNullOrEmpty(token))
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        private bool IsAdmin() =>
            HttpContext.Session.GetString("UserRole") == "Admin";

        // GET /Admin
        public async Task<IActionResult> Index()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");
            SetAuthHeader();

            // Get all users
            var users = new List<JsonElement>();
            try
            {
                var usersResponse = await _httpClient.GetAsync($"{_gatewayUrl}/auth/users");
                if (usersResponse.IsSuccessStatusCode)
                {
                    var usersJson = await usersResponse.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(usersJson) && usersJson != "null")
                        users = JsonSerializer.Deserialize<List<JsonElement>>(usersJson,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }
            }
            catch { }

            // Get all products
            var products = new List<JsonElement>();
            try
            {
                var productsResponse = await _httpClient.GetAsync($"{_gatewayUrl}/products");
                if (productsResponse.IsSuccessStatusCode)
                {
                    var productsJson = await productsResponse.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(productsJson) && productsJson != "null")
                        products = JsonSerializer.Deserialize<List<JsonElement>>(productsJson,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }
            }
            catch { }

            // Get inventory
            var inventory = new List<JsonElement>();
            try
            {
                var inventoryResponse = await _httpClient.GetAsync($"{_gatewayUrl}/inventory");
                if (inventoryResponse.IsSuccessStatusCode)
                {
                    var inventoryJson = await inventoryResponse.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(inventoryJson) && inventoryJson != "null")
                        inventory = JsonSerializer.Deserialize<List<JsonElement>>(inventoryJson,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }
            }
            catch { }

            ViewBag.Users = users;
            ViewBag.Products = products;
            ViewBag.Inventory = inventory;
            return View();
        }

        // POST /Admin/AssignRole
        [HttpPost]
        public async Task<IActionResult> AssignRole(string email, string role)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");
            SetAuthHeader();

            var body = JsonSerializer.Serialize(new { email, role });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync($"{_gatewayUrl}/auth/assignrole", content);
            return RedirectToAction("Index");
        }

        // POST /Admin/DeleteUser
        [HttpPost]
        public async Task<IActionResult> DeleteUser(string email)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");
            SetAuthHeader();
            await _httpClient.DeleteAsync($"{_gatewayUrl}/auth/users/{email}");
            return RedirectToAction("Index");
        }

        // POST /Admin/UpdateStock
        [HttpPost]
        public async Task<IActionResult> UpdateStock(int productId, int stock)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var productsDirectUrl = Environment.GetEnvironmentVariable("PRODUCTS_API_URL")
                ?? "http://localhost:5048";

            var token = HttpContext.Session.GetString("JWTToken");
            var directClient = new HttpClient();
            directClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token!);

            SetAuthHeader();
            var productsResponse = await _httpClient.GetAsync($"{_gatewayUrl}/products");
            var productsJson = await productsResponse.Content.ReadAsStringAsync();
            var products = JsonSerializer.Deserialize<List<JsonElement>>(productsJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

            var product = products.FirstOrDefault(p =>
                p.GetProperty("id").GetInt32() == productId);

            if (product.ValueKind == JsonValueKind.Undefined)
                return RedirectToAction("Index");

            var body = JsonSerializer.Serialize(new
            {
                id = productId,
                name = product.GetProperty("name").GetString(),
                description = product.TryGetProperty("description", out var d) ? d.GetString() : "",
                price = product.GetProperty("price").GetDecimal(),
                stock,
                category = product.TryGetProperty("category", out var c) ? c.GetString() : "General"
            });

            var content = new StringContent(body, Encoding.UTF8, "application/json");
            await directClient.PutAsync($"{productsDirectUrl}/products/{productId}", content);
            return RedirectToAction("Index");
        }
    }
}