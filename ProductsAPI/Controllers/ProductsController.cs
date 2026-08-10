using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductsAPI.Data;
using ProductsAPI.Models;

namespace ProductsAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ProductsDbContext _db;

        public ProductsController(ProductsDbContext db)
        {
            _db = db;
        }

        // GET /products
        // GET /products
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _db.Products.ToListAsync();
            return Ok(products);
        }

        // GET /products/byIds
        [AllowAnonymous]
        [HttpGet("byIds")]
        public async Task<IActionResult> GetByIds([FromQuery] string productIds)
        {
            var ids = productIds.Split(',').Select(int.Parse).ToList();
            var products = await _db.Products
                .Where(p => ids.Contains(p.Id))
                .ToListAsync();
            return Ok(products);
        }

        // POST /products (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Product product)
        {
            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            return Ok(product);
        }

        // DELETE /products/{id} (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();

            _db.Products.Remove(product);
            await _db.SaveChangesAsync();
            return Ok("Product deleted");
        }
        // PUT /products/{id}
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Product product)
        {
            var existing = await _db.Products.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Name = product.Name;
            existing.Description = product.Description;
            existing.Price = product.Price;
            existing.Stock = product.Stock;
            existing.Category = product.Category;
            await _db.SaveChangesAsync();
            return Ok(existing);
        }
        // POST /products/updatestock (Update stock)
        [Authorize]
        [HttpPost("updatestock")]
        public async Task<IActionResult> UpdateStock([FromBody] UpdateStockRequest request)
        {
            var existing = await _db.Products.FindAsync(request.Id);
            if (existing == null) return NotFound();

            existing.Stock = request.Stock;
            await _db.SaveChangesAsync();
            return Ok(existing);
        }

        public class UpdateStockRequest
        {
            public int Id { get; set; }
            public int Stock { get; set; }
        }

    }
}