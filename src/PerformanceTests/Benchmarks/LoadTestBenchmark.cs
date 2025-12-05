using PerformanceTests.Models;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace PerformanceTests.Benchmarks
{
    public class LoadTestBenchmark
    {
        private readonly string _baseUrl;
        private readonly HttpClient _httpClient;

        public LoadTestBenchmark(string baseUrl)
        {
            _baseUrl = baseUrl;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_baseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public async Task<LoadTestResult> RunLoadTest(int requestCount)
        {
            Console.WriteLine($"Running {requestCount} requests for HTTP...");
            var httpStats = await RunLoadTestForEndpoint("/api/orders/http", requestCount);

            Console.WriteLine($"Running {requestCount} requests for gRPC...");
            var grpcStats = await RunLoadTestForEndpoint("/api/orders/grpc", requestCount);

            var performanceGain = ((httpStats.AverageDuration - grpcStats.AverageDuration) / httpStats.AverageDuration) * 100;

            return new LoadTestResult
            {
                HttpStats = httpStats,
                GrpcStats = grpcStats,
                PerformanceGainPercentage = performanceGain
            };
        }

        private async Task<TestStatistics> RunLoadTestForEndpoint(string endpoint, int requestCount)
        {
            var durations = new List<double>();
            var successful = 0;
            var failed = 0;

            var totalStopwatch = Stopwatch.StartNew();

            for (int i = 0; i < requestCount; i++)
            {
                var request = CreateRandomOrder(i);
                var (duration, success) = await SendRequest(endpoint, request);

                durations.Add(duration);
                if (success)
                    successful++;
                else
                    failed++;

                // Progress indicator
                if ((i + 1) % 10 == 0)
                {
                    Console.Write($"\rProgress: {i + 1}/{requestCount}");
                }
            }

            totalStopwatch.Stop();
            Console.WriteLine($"\rProgress: {requestCount}/{requestCount} ✓");

            return new TestStatistics
            {
                TotalRequests = requestCount,
                SuccessfulRequests = successful,
                FailedRequests = failed,
                AverageDuration = durations.Average(),
                MinDuration = durations.Min(),
                MaxDuration = durations.Max(),
                TotalDuration = totalStopwatch.Elapsed.TotalMilliseconds,
                AllDurations = durations
            };
        }

        private async Task<(double Duration, bool Success)> SendRequest(string endpoint, CreateOrderRequest request)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(endpoint, content);
                stopwatch.Stop();

                return (stopwatch.Elapsed.TotalMilliseconds, response.IsSuccessStatusCode);
            }
            catch
            {
                stopwatch.Stop();
                return (stopwatch.Elapsed.TotalMilliseconds, false);
            }
        }

        private CreateOrderRequest CreateRandomOrder(int index)
        {
            var products = new[] { "LAPTOP-001", "PHONE-001", "TABLET-001", "MONITOR-001", "KEYBOARD-001", "MOUSE-001" };
            var random = new Random(index);

            var itemCount = random.Next(1, 4); // 1 to 3 items
            var items = new List<OrderItemRequest>();

            for (int i = 0; i < itemCount; i++)
            {
                items.Add(new OrderItemRequest
                {
                    ProductId = products[random.Next(products.Length)],
                    Quantity = random.Next(1, 5)
                });
            }

            return new CreateOrderRequest
            {
                CustomerId = $"CUST-{index:D6}",
                CustomerEmail = $"customer{index}@example.com",
                Items = items
            };
        }
    }
}
