using PerformanceTests.Models;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace PerformanceTests.Benchmarks
{
    public class HttpVsGrpcBenchmark
    {
        private readonly string _baseUrl;
        private readonly HttpClient _httpClient;

        public HttpVsGrpcBenchmark(string baseUrl)
        {
            _baseUrl = baseUrl;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_baseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public async Task<ComparisonResult> CompareHttpVsGrpc()
        {
            var request = CreateSampleOrder();

            // Test HTTP
            var (httpDuration, httpSuccess) = await TestHttpEndpoint(request);
            await Task.Delay(500); // Small delay between tests

            // Test gRPC
            var (grpcDuration, grpcSuccess) = await TestGrpcEndpoint(request);

            var performanceGain = ((httpDuration - grpcDuration) / httpDuration) * 100;

            return new ComparisonResult
            {
                HttpDuration = httpDuration,
                GrpcDuration = grpcDuration,
                PerformanceGainPercentage = performanceGain,
                HttpSuccess = httpSuccess,
                GrpcSuccess = grpcSuccess
            };
        }

        private async Task<(double Duration, bool Success)> TestHttpEndpoint(CreateOrderRequest request)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/orders/http", content);
                stopwatch.Stop();

                return (stopwatch.Elapsed.TotalMilliseconds, response.IsSuccessStatusCode);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Console.WriteLine($"HTTP Error: {ex.Message}");
                return (stopwatch.Elapsed.TotalMilliseconds, false);
            }
        }

        private async Task<(double Duration, bool Success)> TestGrpcEndpoint(CreateOrderRequest request)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/orders/grpc", content);
                stopwatch.Stop();

                return (stopwatch.Elapsed.TotalMilliseconds, response.IsSuccessStatusCode);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Console.WriteLine($"gRPC Error: {ex.Message}");
                return (stopwatch.Elapsed.TotalMilliseconds, false);
            }
        }

        private CreateOrderRequest CreateSampleOrder()
        {
            return new CreateOrderRequest
            {
                CustomerId = $"CUST-{Guid.NewGuid().ToString("N")[..8]}",
                CustomerEmail = "test@example.com",
                Items = new List<OrderItemRequest>
            {
                new() { ProductId = "LAPTOP-001", Quantity = 1 },
                new() { ProductId = "MOUSE-001", Quantity = 2 }
            }
            };
        }
    }
}
