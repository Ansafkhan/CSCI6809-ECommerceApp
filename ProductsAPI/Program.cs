// Build timestamp: 2026-07-20-v2
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ProductsAPI.Data;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<ProductsDbContext>(options =>
    options.UseSqlite("Data Source=products.db"));

// JWT - same key as AuthAPI
var jwtKey = "ThisIsASecretKeyForJWTToken12345!";
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddControllers();

var app = builder.Build();

// Auto create database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProductsDbContext>();
    db.Database.EnsureCreated();

    if (!db.Products.Any())
    {
        db.Products.AddRange(
            // Electronics
            new ProductsAPI.Models.Product { Name = "Apple iPhone 16 Pro", Description = "Latest iPhone with A18 Pro chip", Price = 1199.99m, Stock = 25, Category = "Electronics" },
            new ProductsAPI.Models.Product { Name = "Dell XPS 15 Laptop", Description = "High performance laptop with RTX 4060", Price = 1799.99m, Stock = 15, Category = "Electronics" },
            new ProductsAPI.Models.Product { Name = "Sony WH-1000XM5 Headphones", Description = "Industry leading noise cancellation", Price = 349.99m, Stock = 30, Category = "Electronics" },
            new ProductsAPI.Models.Product { Name = "Samsung Galaxy Tab S10", Description = "Premium Android tablet with S Pen", Price = 799.99m, Stock = 20, Category = "Electronics" },
            new ProductsAPI.Models.Product { Name = "Apple Watch Series 10", Description = "Advanced health tracking smartwatch", Price = 499.99m, Stock = 18, Category = "Electronics" },
            // Fashion
            new ProductsAPI.Models.Product { Name = "Men's Slim Fit T-Shirt", Description = "Premium cotton comfortable fit", Price = 29.99m, Stock = 100, Category = "Fashion" },
            new ProductsAPI.Models.Product { Name = "Women's Denim Jacket", Description = "Classic style denim jacket", Price = 89.99m, Stock = 50, Category = "Fashion" },
            new ProductsAPI.Models.Product { Name = "Running Sneakers", Description = "Lightweight performance running shoes", Price = 119.99m, Stock = 40, Category = "Fashion" },
            new ProductsAPI.Models.Product { Name = "Leather Wallet", Description = "Genuine leather slim wallet", Price = 49.99m, Stock = 60, Category = "Fashion" },
            new ProductsAPI.Models.Product { Name = "Analog Wrist Watch", Description = "Elegant stainless steel watch", Price = 199.99m, Stock = 25, Category = "Fashion" },
            // Home & Kitchen
            new ProductsAPI.Models.Product { Name = "Air Fryer", Description = "5.5L digital air fryer with 8 presets", Price = 89.99m, Stock = 35, Category = "Home & Kitchen" },
            new ProductsAPI.Models.Product { Name = "Coffee Maker", Description = "12 cup programmable coffee maker", Price = 59.99m, Stock = 45, Category = "Home & Kitchen" },
            new ProductsAPI.Models.Product { Name = "Vacuum Cleaner", Description = "Cordless stick vacuum 25000Pa suction", Price = 149.99m, Stock = 20, Category = "Home & Kitchen" },
            new ProductsAPI.Models.Product { Name = "Blender", Description = "Professional 1200W high speed blender", Price = 79.99m, Stock = 30, Category = "Home & Kitchen" },
            new ProductsAPI.Models.Product { Name = "Microwave Oven", Description = "1000W countertop microwave 1.2 cu ft", Price = 109.99m, Stock = 15, Category = "Home & Kitchen" }
        );
        await db.SaveChangesAsync();
    }
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();