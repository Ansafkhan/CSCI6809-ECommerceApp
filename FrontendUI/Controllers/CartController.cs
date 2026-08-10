using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace FrontendUI.Controllers
{
    public class CartController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string _gatewayUrl;

        public CartController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _gatewayUrl = Environment.GetEnvironmentVariable("GATEWAY_URL")
                ?? "http://localhost:5134";
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

        private string GetUserId()
        {
            return HttpContext.Session.GetString("UserId") ?? "1";
        }

        // GET /Cart
        public async Task<IActionResult> Index()
        {
            var token = HttpContext.Session.GetString("JWTToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            SetAuthHeader();
            var userId = GetUserId();

            try
            {
                var cartResponse = await _httpClient.GetAsync($"{_gatewayUrl}/cart/{userId}");
                var cartJson = await cartResponse.Content.ReadAsStringAsync();

                List<JsonElement> cartItems = new();
                if (cartResponse.IsSuccessStatusCode &&
                    !string.IsNullOrEmpty(cartJson) && cartJson.StartsWith("["))
                {
                    cartItems = JsonSerializer.Deserialize<List<JsonElement>>(cartJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }

                var productsResponse = await _httpClient.GetAsync($"{_gatewayUrl}/products");
                var productsJson = await productsResponse.Content.ReadAsStringAsync();
                var products = JsonSerializer.Deserialize<List<JsonElement>>(productsJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

                decimal total = 0;
                var cartDetails = new List<object>();
                foreach (var item in cartItems)
                {
                    var productId = item.GetProperty("productId").GetInt32();
                    var quantity = item.GetProperty("quantity").GetInt32();
                    var product = products.FirstOrDefault(p =>
                        p.GetProperty("id").GetInt32() == productId);
                    var name = product.ValueKind != JsonValueKind.Undefined
                        ? product.GetProperty("name").GetString() : "Unknown";
                    var price = product.ValueKind != JsonValueKind.Undefined
                        ? product.GetProperty("price").GetDecimal() : 0;
                    total += price * quantity;
                    cartDetails.Add(new
                    {
                        productId,
                        name,
                        quantity,
                        price,
                        subtotal = price * quantity
                    });
                }

                ViewBag.CartDetails = cartDetails;
                ViewBag.Total = total;
                return View();
            }
            catch
            {
                ViewBag.CartDetails = new List<object>();
                ViewBag.Total = 0;
                return View();
            }
        }

        // POST /Cart/AddItem
        [HttpPost]
        public async Task<IActionResult> AddItem(int productId)
        {
            SetAuthHeader();
            var userId = GetUserId();
            var body = JsonSerializer.Serialize(new List<int> { productId });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync($"{_gatewayUrl}/cart/{userId}/items", content);
            return RedirectToAction("Index", "Products");
        }

        // POST /Cart/AddItemAjax
        [HttpPost]
        public async Task<IActionResult> AddItemAjax(int productId)
        {
            var token = HttpContext.Session.GetString("JWTToken");
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Not logged in" });

            SetAuthHeader();
            var userId = GetUserId();
            var body = JsonSerializer.Serialize(new List<int> { productId });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(
                $"{_gatewayUrl}/cart/{userId}/items", content);

            if (response.IsSuccessStatusCode)
                return Json(new { success = true, message = "Added to cart!" });

            return Json(new { success = false, message = "Failed to add" });
        }

        // POST /Cart/Clear
        [HttpPost]
        public async Task<IActionResult> Clear()
        {
            SetAuthHeader();
            var userId = GetUserId();
            await _httpClient.DeleteAsync($"{_gatewayUrl}/cart/{userId}/items");
            return RedirectToAction("Index");
        }

        // POST /Cart/Purchase
        [HttpPost]
        public async Task<IActionResult> Purchase()
        {
            var token = HttpContext.Session.GetString("JWTToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            SetAuthHeader();
            var userId = GetUserId();

            var cartResponse = await _httpClient.GetAsync($"{_gatewayUrl}/cart/{userId}");
            var cartJson = await cartResponse.Content.ReadAsStringAsync();
            var cartItems = JsonSerializer.Deserialize<List<JsonElement>>(cartJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

            if (!cartItems.Any())
                return RedirectToAction("Index");

            var productsResponse = await _httpClient.GetAsync($"{_gatewayUrl}/products");
            var productsJson = await productsResponse.Content.ReadAsStringAsync();
            var products = JsonSerializer.Deserialize<List<JsonElement>>(productsJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

            decimal total = 0;
            var purchasedItems = new List<object>();
            var productsDirectUrl = Environment.GetEnvironmentVariable("PRODUCTS_API_URL")
                ?? "http://localhost:5048";

            foreach (var item in cartItems)
            {
                var productId = item.GetProperty("productId").GetInt32();
                var quantity = item.GetProperty("quantity").GetInt32();
                var product = products.FirstOrDefault(p =>
                    p.GetProperty("id").GetInt32() == productId);

                if (product.ValueKind == JsonValueKind.Undefined) continue;

                var name = product.GetProperty("name").GetString() ?? "";
                var price = product.GetProperty("price").GetDecimal();
                var currentStock = product.GetProperty("stock").GetInt32();
                var newStock = Math.Max(0, currentStock - quantity);
                var category = product.TryGetProperty("category", out var cat)
                    ? cat.GetString() : "General";
                var description = product.TryGetProperty("description", out var desc)
                    ? desc.GetString() : "";

                total += price * quantity;
                purchasedItems.Add(new
                {
                    name,
                    quantity,
                    price,
                    subtotal = price * quantity
                });

                var updateBody = JsonSerializer.Serialize(new
                {
                    id = productId,
                    name,
                    description,
                    price,
                    stock = newStock,
                    category
                });

                var updateContent = new StringContent(
                    updateBody, Encoding.UTF8, "application/json");

                var directClient = new HttpClient();
                directClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Bearer", token);

                var updateResponse = await directClient.PutAsync(
                    $"{productsDirectUrl}/products/{productId}", updateContent);

                Console.WriteLine($"Stock update for {name}: {currentStock} -> {newStock}, Status: {updateResponse.StatusCode}");
            }

            await _httpClient.DeleteAsync($"{_gatewayUrl}/cart/{userId}/items");

            ViewBag.PurchasedItems = purchasedItems;
            ViewBag.Total = total;
            return View();
        }
    }
}