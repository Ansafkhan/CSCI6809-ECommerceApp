using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using InventoryAPI.Data;
using InventoryAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseSqlite("Data Source=inventory.db"));

// JWT
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
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey))
    };
});

// RabbitMQ Consumer background service
builder.Services.AddHostedService<RabbitMQConsumer>();
builder.Services.AddControllers();

var app = builder.Build();

// Auto create database and seed some inventory
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    db.Database.EnsureCreated();

    // Seed initial inventory if empty
    if (!db.InventoryItems.Any())
    {
        db.InventoryItems.AddRange(
            new InventoryAPI.Models.InventoryItem { ProductId = 1, ProductName = "Apple iPhone 16 Pro", Stock = 25 },
            new InventoryAPI.Models.InventoryItem { ProductId = 2, ProductName = "Dell XPS 15 Laptop", Stock = 15 },
            new InventoryAPI.Models.InventoryItem { ProductId = 3, ProductName = "Sony WH-1000XM5 Headphones", Stock = 30 },
            new InventoryAPI.Models.InventoryItem { ProductId = 4, ProductName = "Samsung Galaxy Tab S10", Stock = 20 },
            new InventoryAPI.Models.InventoryItem { ProductId = 5, ProductName = "Apple Watch Series 10", Stock = 18 },
            new InventoryAPI.Models.InventoryItem { ProductId = 6, ProductName = "Men's Slim Fit T-Shirt", Stock = 100 },
            new InventoryAPI.Models.InventoryItem { ProductId = 7, ProductName = "Women's Denim Jacket", Stock = 50 },
            new InventoryAPI.Models.InventoryItem { ProductId = 8, ProductName = "Running Sneakers", Stock = 40 },
            new InventoryAPI.Models.InventoryItem { ProductId = 9, ProductName = "Leather Wallet", Stock = 60 },
            new InventoryAPI.Models.InventoryItem { ProductId = 10, ProductName = "Analog Wrist Watch", Stock = 25 },
            new InventoryAPI.Models.InventoryItem { ProductId = 11, ProductName = "Air Fryer", Stock = 35 },
            new InventoryAPI.Models.InventoryItem { ProductId = 12, ProductName = "Coffee Maker", Stock = 45 },
            new InventoryAPI.Models.InventoryItem { ProductId = 13, ProductName = "Vacuum Cleaner", Stock = 20 },
            new InventoryAPI.Models.InventoryItem { ProductId = 14, ProductName = "Blender", Stock = 30 },
            new InventoryAPI.Models.InventoryItem { ProductId = 15, ProductName = "Microwave Oven", Stock = 15 }
        );
        await db.SaveChangesAsync();
    }
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();