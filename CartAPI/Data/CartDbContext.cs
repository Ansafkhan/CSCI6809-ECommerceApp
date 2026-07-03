using Microsoft.EntityFrameworkCore;
using CartAPI.Models;

namespace CartAPI.Data
{
    public class CartDbContext : DbContext
    {
        public CartDbContext(DbContextOptions<CartDbContext> options)
            : base(options) { }

        public DbSet<CartItem> CartItems { get; set; }
    }
}