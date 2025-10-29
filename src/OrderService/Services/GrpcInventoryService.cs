using Grpc.Net.Client;
using InventoryService.Grpc;
using System.Diagnostics;

namespace OrderService.Services
{
    public class GrpcInventoryService : IInventoryService
    {
        private readonly ILogger<GrpcInventoryService> _logger;
        private readonly string _grpcUrl;
        private readonly GrpcChannel _channel;
        private readonly InventoryService.Grpc.InventoryService.InventoryServiceClient _client;

        public GrpcInventoryService(
            IConfiguration configuration,
            ILogger<GrpcInventoryService> logger)
        {
            _logger = logger;
            _grpcUrl = configuration["InventoryService:GrpcUrl"] ?? "http://localhost:5012";

            // Create gRPC channel
            _channel = GrpcChannel.ForAddress(_grpcUrl, new GrpcChannelOptions
            {
                HttpHandler = new SocketsHttpHandler
                {
                    PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
                    KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                    EnableMultipleHttp2Connections = true
                }
            });

            _client = new InventoryService.Grpc.InventoryService.InventoryServiceClient(_channel);

            _logger.LogInformation("gRPC client initialized for {GrpcUrl}", _grpcUrl);
        }

        public async Task<(bool IsAvailable, int AvailableQuantity, double DurationMs)> CheckAvailabilityAsync(
            string productId, int quantity)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("gRPC: Checking availability for {ProductId} (quantity: {Quantity})",
                    productId, quantity);

                var request = new CheckAvailabilityRequest
                {
                    ProductId = productId,
                    Quantity = quantity
                };

                var response = await _client.CheckAvailabilityAsync(request);
                stopwatch.Stop();

                _logger.LogInformation("gRPC: Check availability completed in {Duration}ms - Available: {IsAvailable}",
                    stopwatch.Elapsed.TotalMilliseconds, response.IsAvailable);

                return (response.IsAvailable, response.AvailableQuantity, stopwatch.Elapsed.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "gRPC: Error checking availability for {ProductId}", productId);
                return (false, 0, stopwatch.Elapsed.TotalMilliseconds);
            }
        }

        public async Task<(bool Success, string Message, double DurationMs)> ReserveItemsAsync(
            string productId, int quantity, string orderId)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("gRPC: Reserving {Quantity} units of {ProductId} for order {OrderId}",
                    quantity, productId, orderId);

                var request = new ReserveItemsRequest
                {
                    ProductId = productId,
                    Quantity = quantity,
                    OrderId = orderId
                };

                var response = await _client.ReserveItemsAsync(request);
                stopwatch.Stop();

                _logger.LogInformation("gRPC: Reserve items completed in {Duration}ms - Success: {Success}",
                    stopwatch.Elapsed.TotalMilliseconds, response.Success);

                return (response.Success, response.Message, stopwatch.Elapsed.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "gRPC: Error reserving items for {ProductId}", productId);
                return (false, ex.Message, stopwatch.Elapsed.TotalMilliseconds);
            }
        }

        public async Task<(bool Success, double DurationMs)> ReleaseItemsAsync(
            string productId, int quantity, string orderId)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("gRPC: Releasing {Quantity} units of {ProductId} for order {OrderId}",
                    quantity, productId, orderId);

                var request = new ReleaseItemsRequest
                {
                    ProductId = productId,
                    Quantity = quantity,
                    OrderId = orderId
                };

                var response = await _client.ReleaseItemsAsync(request);
                stopwatch.Stop();

                _logger.LogInformation("gRPC: Release items completed in {Duration}ms - Success: {Success}",
                    stopwatch.Elapsed.TotalMilliseconds, response.Success);

                return (response.Success, stopwatch.Elapsed.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "gRPC: Error releasing items for {ProductId}", productId);
                return (false, stopwatch.Elapsed.TotalMilliseconds);
            }
        }
    }
}
