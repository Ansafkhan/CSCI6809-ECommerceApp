using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CartAPI.Data;
using CartAPI.Models;
using System.Text.Json;

namespace CartAPI.Controllers
{
    [ApiController]
    [Route("cart")]
    public class CartController : ControllerBase
    {
        private readonly CartDbContext _db;
        private readonly HttpClient _httpClient;

        public CartController(CartDbContext db, IHttpClientFactory httpClientFactory)
        {
            _db = db;
            _httpClient = httpClientFactory.CreateClient();
        }

        // GET /cart/{userid}
        [Authorize]
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetCart(int userId)
        {
            var items = await _db.CartItems
                .Where(c => c.UserId == userId)
                .ToListAsync();
            return Ok(items);
        }

        // POST /cart/{userid}/items
        [Authorize]
        [HttpPost("{userId}/items")]
        public async Task<IActionResult> AddItems(int userId, [FromBody] List<int> productIds)
        {
            // Get JWT token from request to forward to other services
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            // Call ProductsAPI to get product details
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var productResponse = await _httpClient.GetAsync(
                $"http://localhost:5048/products/byIds?productIds={string.Join(",", productIds)}");

            if (!productResponse.IsSuccessStatusCode)
                return BadRequest("Could not fetch products");

            var productsJson = await productResponse.Content.ReadAsStringAsync();
            var products = JsonSerializer.Deserialize<List<ProductDto>>(productsJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Add items to cart
            foreach (var productId in productIds)
            {
                var existing = await _db.CartItems
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

                if (existing != null)
                    existing.Quantity++;
                else
                    _db.CartItems.Add(new CartItem
                    {
                        UserId = userId,
                        ProductId = productId,
                        Quantity = 1
                    });
            }
            await _db.SaveChangesAsync();

            // Call PriceAPI to get total price
            var cartItems = await _db.CartItems
                .Where(c => c.UserId == userId)
                .ToListAsync();

            var priceResponse = await _httpClient.PostAsJsonAsync(
                "http://localhost:5097/price", cartItems);

            var priceJson = await priceResponse.Content.ReadAsStringAsync();
            var priceData = System.Text.Json.JsonSerializer.Deserialize<object>(priceJson);

            return Ok(new
            {
                cart = cartItems,
                products = products,
                price = priceData
            });
        }

        // DELETE /cart/{userid}/items
        [Authorize]
        [HttpDelete("{userId}/items")]
        public async Task<IActionResult> ClearCart(int userId)
        {
            var items = await _db.CartItems
                .Where(c => c.UserId == userId)
                .ToListAsync();

            _db.CartItems.RemoveRange(items);
            await _db.SaveChangesAsync();
            return Ok("Cart cleared");
        }
    }

    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
    }
}