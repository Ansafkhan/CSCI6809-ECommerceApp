namespace InventoryAPI.Models
{
    public class InventoryItem
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Stock { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}