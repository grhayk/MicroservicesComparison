using PerformanceTests.Models;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace PerformanceTests.Benchmarks
{
    public class ConcurrencyBenchmark
    {
        private readonly string _baseUrl;
        private readonly HttpClient _httpClient;

        public ConcurrencyBenchmark(string baseUrl)
        {
            _baseUrl = baseUrl;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_baseUrl),
                Timeout = TimeSpan.FromSeconds(60)
            };
        }

        public async Task<ConcurrencyResult> RunSequentialTest(int requestCount)
        {
            var (httpDuration, httpSuccess) = await RunSequential("/api/orders/http", requestCount);
            var (grpcDuration, grpcSuccess) = await RunSequential("/api/orders/grpc", requestCount);

            var performanceGain = ((httpDuration - grpcDuration) / httpDuration) * 100;

            return new ConcurrencyResult
            {
                HttpTotalDuration = httpDuration,
                GrpcTotalDuration = grpcDuration,
                HttpSuccessRate = httpSuccess,
                GrpcSuccessRate = grpcSuccess,
                PerformanceGainPercentage = performanceGain
            };
        }

        public async Task<ConcurrencyResult> RunConcurrentTest(int requestCount)
        {
            var (httpDuration, httpSuccess) = await RunConcurrent("/api/orders/http", requestCount);
            var (grpcDuration, grpcSuccess) = await RunConcurrent("/api/orders/grpc", requestCount);

            var performanceGain = ((httpDuration - grpcDuration) / httpDuration) * 100;

            return new ConcurrencyResult
            {
                HttpTotalDuration = httpDuration,
                GrpcTotalDuration = grpcDuration,
                HttpSuccessRate = httpSuccess,
                GrpcSuccessRate = grpcSuccess,
                PerformanceGainPercentage = performanceGain
            };
        }

        private async Task<(double TotalDuration, double SuccessRate)> RunSequential(string endpoint, int requestCount)
        {
            Console.WriteLine($"Running {requestCount} sequential requests for {endpoint}...");

            var stopwatch = Stopwatch.StartNew();
            var successful = 0;

            for (int i = 0; i < requestCount; i++)
            {
                var request = CreateOrder(i);
                var success = await SendRequest(endpoint, request);

                if (success)
                    successful++;

                Console.Write($"\rProgress: {i + 1}/{requestCount}");
            }

            stopwatch.Stop();
            Console.WriteLine($"\rProgress: {requestCount}/{requestCount} ✓");

            var successRate = (successful / (double)requestCount) * 100;
            return (stopwatch.Elapsed.TotalMilliseconds, successRate);
        }

        private async Task<(double TotalDuration, double SuccessRate)> RunConcurrent(string endpoint, int requestCount)
        {
            Console.WriteLine($"Running {requestCount} concurrent requests for {endpoint}...");

            var stopwatch = Stopwatch.StartNew();

            var tasks = new List<Task<bool>>();
            for (int i = 0; i < requestCount; i++)
            {
                var request = CreateOrder(i);
                tasks.Add(SendRequest(endpoint, request));
            }

            var results = await Task.WhenAll(tasks);
            stopwatch.Stop();

            var successful = results.Count(r => r);
            var successRate = (successful / (double)requestCount) * 100;

            Console.WriteLine($"Completed: {requestCount} requests ✓");

            return (stopwatch.Elapsed.TotalMilliseconds, successRate);
        }

        private async Task<bool> SendRequest(string endpoint, CreateOrderRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(endpoint, content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private CreateOrderRequest CreateOrder(int index)
        {
            return new CreateOrderRequest
            {
                CustomerId = $"CUST-CONC-{index:D4}",
                CustomerEmail = $"concurrent{index}@example.com",
                Items = new List<OrderItemRequest>
            {
                new() { ProductId = "LAPTOP-001", Quantity = 1 }
            }
            };
        }
    }
}
