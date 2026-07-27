using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromSeconds(10);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 5;
    });
});

// Health Checks
builder.Services.AddHealthChecks()
    .AddUrlGroup(new Uri("http://localhost:5155/health"), name: "AuthAPI")
    .AddUrlGroup(new Uri("http://localhost:5048/health"), name: "ProductsAPI")
    .AddUrlGroup(new Uri("http://localhost:5106/health"), name: "CartAPI")
    .AddUrlGroup(new Uri("http://localhost:5097/health"), name: "PriceAPI");

var app = builder.Build();

app.UseRateLimiter();
app.MapReverseProxy();
app.MapHealthChecks("/health");

app.Run();


// docker compose stop frontend-ui
// docker compose build --no-cache frontend-ui
// docker compose up -d frontend-ui