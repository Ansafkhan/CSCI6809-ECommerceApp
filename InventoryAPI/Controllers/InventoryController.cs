using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryAPI.Data;
using InventoryAPI.Models;

namespace InventoryAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly InventoryDbContext _db;

        public InventoryController(InventoryDbContext db)
        {
            _db = db;
        }

        // GET /inventory
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _db.InventoryItems.ToListAsync();
            return Ok(items);
        }

        // GET /inventory/{productId}
        [Authorize]
        [HttpGet("{productId}")]
        public async Task<IActionResult> GetByProductId(int productId)
        {
            var item = await _db.InventoryItems
                .FirstOrDefaultAsync(i => i.ProductId == productId);
            if (item == null) return NotFound();
            return Ok(item);
        }

        // PUT /inventory/{productId} (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpPut("{productId}")]
        public async Task<IActionResult> UpdateStock(int productId,
            [FromBody] UpdateStockDto dto)
        {
            var item = await _db.InventoryItems
                .FirstOrDefaultAsync(i => i.ProductId == productId);
            if (item == null) return NotFound();

            item.Stock = dto.Stock;
            item.LastUpdated = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(item);
        }
    }

    public record UpdateStockDto(int Stock);
}