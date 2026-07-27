using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using InventoryAPI.Data;
using InventoryAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Services
{
    public class RabbitMQConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private IConnection? _connection;
        private IChannel? _channel;

        public RabbitMQConsumer(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Wait for RabbitMQ to be ready
            await Task.Delay(3000, stoppingToken);

            try
            {
                var rabbitHost = Environment.GetEnvironmentVariable("RabbitMQ__Host") ?? "localhost";

                var factory = new ConnectionFactory
                {
                    HostName = rabbitHost,
                    Port = 5672,
                    UserName = "guest",
                    Password = "guest"
                };

                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();

                await _channel.QueueDeclareAsync(
                    queue: "cart_items",
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    Console.WriteLine($"Received message: {message}");

                    try
                    {
                        var cartMessage = JsonSerializer.Deserialize<CartMessage>(message,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (cartMessage != null)
                        {
                            using var scope = _scopeFactory.CreateScope();
                            var db = scope.ServiceProvider
                                .GetRequiredService<InventoryDbContext>();

                            var item = await db.InventoryItems
                                .FirstOrDefaultAsync(i => i.ProductId == cartMessage.ProductId);

                            if (item != null)
                            {
                                item.Stock = Math.Max(0, item.Stock - cartMessage.Quantity);
                                item.LastUpdated = DateTime.UtcNow;
                                await db.SaveChangesAsync();
                                Console.WriteLine($"Stock updated for product {cartMessage.ProductId}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing message: {ex.Message}");
                    }

                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                };

                await _channel.BasicConsumeAsync(
                    queue: "cart_items",
                    autoAck: false,
                    consumer: consumer
                );

                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RabbitMQ connection error: {ex.Message}");
            }
        }

        public override void Dispose()
        {
            _channel?.CloseAsync();
            _connection?.CloseAsync();
            base.Dispose();
        }
    }

    public class CartMessage
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public int UserId { get; set; }
    }
}