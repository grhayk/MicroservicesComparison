using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace OrderService.Services
{
    public class HttpInventoryService : IInventoryService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HttpInventoryService> _logger;
        private readonly string _baseUrl;

        public HttpInventoryService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<HttpInventoryService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _baseUrl = configuration["InventoryService:HttpUrl"] ?? "http://localhost:5120";
            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<(bool IsAvailable, int AvailableQuantity, double DurationMs)> CheckAvailabilityAsync(
            string productId, int quantity)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var request = new { ProductId = productId, Quantity = quantity };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("HTTP: Checking availability for {ProductId} (quantity: {Quantity})",
                    productId, quantity);

                var response = await _httpClient.PostAsync("/api/inventory/check", content);
                stopwatch.Stop();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("HTTP: Failed to check availability. Status: {StatusCode}",
                        response.StatusCode);
                    return (false, 0, stopwatch.Elapsed.TotalMilliseconds);
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<StockCheckResponse>(responseJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                _logger.LogInformation("HTTP: Check availability completed in {Duration}ms - Available: {IsAvailable}",
                    stopwatch.Elapsed.TotalMilliseconds, result?.IsAvailable ?? false);

                return (
                    result?.IsAvailable ?? false,
                    result?.AvailableQuantity ?? 0,
                    stopwatch.Elapsed.TotalMilliseconds
                );
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "HTTP: Error checking availability for {ProductId}", productId);
                return (false, 0, stopwatch.Elapsed.TotalMilliseconds);
            }
        }

        public async Task<(bool Success, string Message, double DurationMs)> ReserveItemsAsync(
            string productId, int quantity, string orderId)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var request = new { ProductId = productId, Quantity = quantity, OrderId = orderId };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("HTTP: Reserving {Quantity} units of {ProductId} for order {OrderId}",
                    quantity, productId, orderId);

                var response = await _httpClient.PostAsync("/api/inventory/reserve", content);
                stopwatch.Stop();

                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ReserveStockResponse>(responseJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                _logger.LogInformation("HTTP: Reserve items completed in {Duration}ms - Success: {Success}",
                    stopwatch.Elapsed.TotalMilliseconds, result?.Success ?? false);

                return (
                    result?.Success ?? false,
                    result?.Message ?? "Unknown error",
                    stopwatch.Elapsed.TotalMilliseconds
                );
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "HTTP: Error reserving items for {ProductId}", productId);
                return (false, ex.Message, stopwatch.Elapsed.TotalMilliseconds);
            }
        }

        public async Task<(bool Success, double DurationMs)> ReleaseItemsAsync(
            string productId, int quantity, string orderId)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var request = new { ProductId = productId, Quantity = quantity, OrderId = orderId };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("HTTP: Releasing {Quantity} units of {ProductId} for order {OrderId}",
                    quantity, productId, orderId);

                var response = await _httpClient.PostAsync("/api/inventory/release", content);
                stopwatch.Stop();

                var success = response.IsSuccessStatusCode;

                _logger.LogInformation("HTTP: Release items completed in {Duration}ms - Success: {Success}",
                    stopwatch.Elapsed.TotalMilliseconds, success);

                return (success, stopwatch.Elapsed.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "HTTP: Error releasing items for {ProductId}", productId);
                return (false, stopwatch.Elapsed.TotalMilliseconds);
            }
        }

        private class StockCheckResponse
        {
            public bool IsAvailable { get; set; }
            public int AvailableQuantity { get; set; }
            public string Message { get; set; } = string.Empty;
        }

        private class ReserveStockResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public int RemainingQuantity { get; set; }
        }
    }
}
