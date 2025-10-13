using InventoryService.Models;
using System.Collections.Concurrent;

namespace InventoryService.Data
{
    public interface IInventoryRepository
    {
        Task<InventoryItem?> GetItemAsync(string productId);
        Task<IEnumerable<InventoryItem>> GetAllItemsAsync();
        Task<bool> CheckAvailabilityAsync(string productId, int quantity);
        Task<bool> ReserveItemsAsync(string productId, int quantity);
        Task<bool> ReleaseItemsAsync(string productId, int quantity);
        Task<int> GetAvailableQuantityAsync(string productId);
    }

    public class InventoryRepository : IInventoryRepository
    {
        private readonly ConcurrentDictionary<string, InventoryItem> _inventory;
        private readonly ILogger<InventoryRepository> _logger;

        public InventoryRepository(ILogger<InventoryRepository> logger)
        {
            _logger = logger;
            _inventory = new ConcurrentDictionary<string, InventoryItem>();

            // Initialize with sample data
            InitializeSampleData();
        }

        private void InitializeSampleData()
        {
            var items = new List<InventoryItem>
            {
                new() { ProductId = "LAPTOP-001", Name = "Dell XPS 15", Quantity = 50, Price = 1299.99m, LastUpdated = DateTime.UtcNow },
                new() { ProductId = "PHONE-001", Name = "iPhone 15 Pro", Quantity = 100, Price = 999.99m, LastUpdated = DateTime.UtcNow },
                new() { ProductId = "TABLET-001", Name = "iPad Air", Quantity = 75, Price = 599.99m, LastUpdated = DateTime.UtcNow },
                new() { ProductId = "MONITOR-001", Name = "LG UltraWide", Quantity = 30, Price = 399.99m, LastUpdated = DateTime.UtcNow },
                new() { ProductId = "KEYBOARD-001", Name = "Mechanical Keyboard", Quantity = 200, Price = 149.99m, LastUpdated = DateTime.UtcNow },
                new() { ProductId = "MOUSE-001", Name = "Logitech MX Master", Quantity = 150, Price = 99.99m, LastUpdated = DateTime.UtcNow },
            };

            foreach (var item in items)
            {
                _inventory.TryAdd(item.ProductId, item);
            }

            _logger.LogInformation("Initialized inventory with {Count} items", items.Count);
        }

        public Task<InventoryItem?> GetItemAsync(string productId)
        {
            _inventory.TryGetValue(productId, out var item);
            return Task.FromResult(item);
        }

        public Task<IEnumerable<InventoryItem>> GetAllItemsAsync()
        {
            return Task.FromResult<IEnumerable<InventoryItem>>(_inventory.Values.ToList());
        }

        public Task<bool> CheckAvailabilityAsync(string productId, int quantity)
        {
            if (!_inventory.TryGetValue(productId, out var item))
            {
                _logger.LogWarning("Product {ProductId} not found", productId);
                return Task.FromResult(false);
            }

            var isAvailable = item.Quantity >= quantity;
            _logger.LogInformation("Availability check for {ProductId}: {IsAvailable} (requested: {Quantity}, available: {Available})",
                productId, isAvailable, quantity, item.Quantity);

            return Task.FromResult(isAvailable);
        }

        public Task<bool> ReserveItemsAsync(string productId, int quantity)
        {
            if (!_inventory.TryGetValue(productId, out var item))
            {
                _logger.LogWarning("Cannot reserve - Product {ProductId} not found", productId);
                return Task.FromResult(false);
            }

            if (item.Quantity < quantity)
            {
                _logger.LogWarning("Cannot reserve - Insufficient stock for {ProductId}. Requested: {Quantity}, Available: {Available}",
                    productId, quantity, item.Quantity);
                return Task.FromResult(false);
            }

            item.Quantity -= quantity;
            item.LastUpdated = DateTime.UtcNow;

            _logger.LogInformation("Reserved {Quantity} units of {ProductId}. Remaining: {Remaining}",
                quantity, productId, item.Quantity);

            return Task.FromResult(true);
        }

        public Task<bool> ReleaseItemsAsync(string productId, int quantity)
        {
            if (!_inventory.TryGetValue(productId, out var item))
            {
                _logger.LogWarning("Cannot release - Product {ProductId} not found", productId);
                return Task.FromResult(false);
            }

            item.Quantity += quantity;
            item.LastUpdated = DateTime.UtcNow;

            _logger.LogInformation("Released {Quantity} units of {ProductId}. New quantity: {NewQuantity}",
                quantity, productId, item.Quantity);

            return Task.FromResult(true);
        }

        public Task<int> GetAvailableQuantityAsync(string productId)
        {
            if (!_inventory.TryGetValue(productId, out var item))
            {
                return Task.FromResult(0);
            }

            return Task.FromResult(item.Quantity);
        }
    }
}
