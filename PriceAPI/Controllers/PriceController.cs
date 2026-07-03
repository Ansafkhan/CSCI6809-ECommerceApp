using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace PriceAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PriceController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public PriceController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        // POST /price
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> GetPrice([FromBody] List<CartItemDto> cartItems)
        {
            // Get JWT token from request
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            // Call ProductsAPI to get product prices
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var productIds = cartItems.Select(c => c.ProductId).ToList();
            var productResponse = await _httpClient.GetAsync(
                $"http://localhost:5048/products/byIds?productIds={string.Join(",", productIds)}");

            if (!productResponse.IsSuccessStatusCode)
                return BadRequest("Could not fetch products");

            var productsJson = await productResponse.Content.ReadAsStringAsync();
            var products = JsonSerializer.Deserialize<List<ProductDto>>(productsJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Calculate total price
            decimal total = 0;
            foreach (var item in cartItems)
            {
                var product = products?.FirstOrDefault(p => p.Id == item.ProductId);
                if (product != null)
                    total += product.Price * item.Quantity;
            }

            return Ok(new { totalPrice = total });
        }
    }

    public class CartItemDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
    }
}