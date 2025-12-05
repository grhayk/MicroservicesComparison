using PerformanceTests.Benchmarks;
using PerformanceTests.Models;
using System.Text.Json;

Console.WriteLine("═══════════════════════════════════════════════════════");
Console.WriteLine("🚀 Microservices Performance Comparison Tool");
Console.WriteLine("═══════════════════════════════════════════════════════");
Console.WriteLine();

var orderServiceUrl = "http://localhost:5202";

// Check if services are available
if (!await CheckServicesAvailability(orderServiceUrl))
{
    Console.WriteLine("⚠️  Services are not available. Please start the services first.");
    Console.WriteLine();
    Console.WriteLine("Run: docker-compose up");
    Console.WriteLine("Or run services individually from Visual Studio");
    return;
}

Console.WriteLine("✅ All services are available");
Console.WriteLine();

while (true)
{
    ShowMenu();
    var choice = Console.ReadLine();

    switch (choice)
    {
        case "1":
            await RunHttpVsGrpcComparison(orderServiceUrl);
            break;
        case "2":
            await RunLoadTest(orderServiceUrl);
            break;
        case "3":
            await RunSequentialTest(orderServiceUrl);
            break;
        case "4":
            await RunConcurrentTest(orderServiceUrl);
            break;
        case "5":
            await RunFullBenchmarkSuite(orderServiceUrl);
            break;
        case "6":
            Console.WriteLine("Goodbye! 👋");
            return;
        default:
            Console.WriteLine("Invalid choice. Please try again.");
            break;
    }

    Console.WriteLine();
    Console.WriteLine("Press any key to continue...");
    Console.ReadKey();
    Console.Clear();
}


static void ShowMenu()
{
    Console.WriteLine("═══════════════════════════════════════════════════════");
    Console.WriteLine("Select Test:");
    Console.WriteLine("═══════════════════════════════════════════════════════");
    Console.WriteLine("1. HTTP vs gRPC Comparison (Single Request)");
    Console.WriteLine("2. Load Test (100 requests each)");
    Console.WriteLine("3. Sequential Test (One by one)");
    Console.WriteLine("4. Concurrent Test (Parallel execution)");
    Console.WriteLine("5. Full Benchmark Suite (All tests)");
    Console.WriteLine("6. Exit");
    Console.WriteLine("═══════════════════════════════════════════════════════");
    Console.Write("Your choice: ");
}

static async Task<bool> CheckServicesAvailability(string baseUrl)
{
    try
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var response = await client.GetAsync($"{baseUrl}/health");
        return response.IsSuccessStatusCode;
    }
    catch
    {
        return false;
    }
}

static async Task RunHttpVsGrpcComparison(string baseUrl)
{
    Console.WriteLine();
    Console.WriteLine("🔬 Running HTTP vs gRPC Comparison...");
    Console.WriteLine();

    var tester = new HttpVsGrpcBenchmark(baseUrl);
    var results = await tester.CompareHttpVsGrpc();

    DisplayComparisonResults(results);
}

static async Task RunLoadTest(string baseUrl)
{
    Console.WriteLine();
    Console.WriteLine("🔬 Running Load Test (100 requests each)...");
    Console.WriteLine();

    var tester = new LoadTestBenchmark(baseUrl);
    var results = await tester.RunLoadTest(requestCount: 100);

    DisplayLoadTestResults(results);
}

static async Task RunSequentialTest(string baseUrl)
{
    Console.WriteLine();
    Console.WriteLine("🔬 Running Sequential Test (10 requests)...");
    Console.WriteLine();

    var tester = new ConcurrencyBenchmark(baseUrl);
    var results = await tester.RunSequentialTest(requestCount: 10);

    DisplayConcurrencyResults(results, "Sequential");
}

static async Task RunConcurrentTest(string baseUrl)
{
    Console.WriteLine();
    Console.WriteLine("🔬 Running Concurrent Test (10 requests in parallel)...");
    Console.WriteLine();

    var tester = new ConcurrencyBenchmark(baseUrl);
    var results = await tester.RunConcurrentTest(requestCount: 10);

    DisplayConcurrencyResults(results, "Concurrent");
}

static async Task RunFullBenchmarkSuite(string baseUrl)
{
    Console.WriteLine();
    Console.WriteLine("🔬 Running Full Benchmark Suite...");
    Console.WriteLine("This will take several minutes...");
    Console.WriteLine();

    var allResults = new List<string>();

    // Test 1: Single request comparison
    Console.WriteLine("Test 1/4: HTTP vs gRPC Single Request");
    var tester1 = new HttpVsGrpcBenchmark(baseUrl);
    var result1 = await tester1.CompareHttpVsGrpc();
    allResults.Add($"Single Request - HTTP: {result1.HttpDuration:F2}ms, gRPC: {result1.GrpcDuration:F2}ms");

    // Test 2: Load test
    Console.WriteLine("Test 2/4: Load Test (100 requests)");
    var tester2 = new LoadTestBenchmark(baseUrl);
    var result2 = await tester2.RunLoadTest(100);
    allResults.Add($"Load Test (100) - HTTP: {result2.HttpStats.AverageDuration:F2}ms, gRPC: {result2.GrpcStats.AverageDuration:F2}ms");

    // Test 3: Sequential test
    Console.WriteLine("Test 3/4: Sequential Test");
    var tester3 = new ConcurrencyBenchmark(baseUrl);
    var result3 = await tester3.RunSequentialTest(10);
    allResults.Add($"Sequential - HTTP: {result3.HttpTotalDuration:F2}ms, gRPC: {result3.GrpcTotalDuration:F2}ms");

    // Test 4: Concurrent test
    Console.WriteLine("Test 4/4: Concurrent Test");
    var result4 = await tester3.RunConcurrentTest(10);
    allResults.Add($"Concurrent - HTTP: {result4.HttpTotalDuration:F2}ms, gRPC: {result4.GrpcTotalDuration:F2}ms");

    Console.WriteLine();
    Console.WriteLine("═══════════════════════════════════════════════════════");
    Console.WriteLine("📊 FULL BENCHMARK SUITE RESULTS");
    Console.WriteLine("═══════════════════════════════════════════════════════");
    foreach (var result in allResults)
    {
        Console.WriteLine($"  {result}");
    }
    Console.WriteLine("═══════════════════════════════════════════════════════");

    // Save to file
    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
    var filename = $"benchmark_results_{timestamp}.json";

    var fullResults = new
    {
        Timestamp = DateTime.Now,
        SingleRequest = result1,
        LoadTest = result2,
        Sequential = result3,
        Concurrent = result4
    };

    var json = JsonSerializer.Serialize(fullResults, new JsonSerializerOptions { WriteIndented = true });
    await File.WriteAllTextAsync(filename, json);

    Console.WriteLine();
    Console.WriteLine($"✅ Results saved to: {filename}");
}

static void DisplayComparisonResults(ComparisonResult result)
{
    Console.WriteLine("═══════════════════════════════════════════════════════");
    Console.WriteLine("📊 COMPARISON RESULTS");
    Console.WriteLine("═══════════════════════════════════════════════════════");
    Console.WriteLine($"HTTP Duration:     {result.HttpDuration:F2} ms");
    Console.WriteLine($"gRPC Duration:     {result.GrpcDuration:F2} ms");
    Console.WriteLine($"Difference:        {Math.Abs(result.HttpDuration - result.GrpcDuration):F2} ms");
    Console.WriteLine($"Performance Gain:  {result.PerformanceGainPercentage:F2}%");
    Console.WriteLine($"Winner:            {(result.HttpDuration < result.GrpcDuration ? "HTTP ⚡" : "gRPC ⚡")}");
    Console.WriteLine("═══════════════════════════════════════════════════════");
}

static void DisplayLoadTestResults(LoadTestResult result)
{
    Console.WriteLine("═══════════════════════════════════════════════════════");
    Console.WriteLine("📊 LOAD TEST RESULTS");
    Console.WriteLine("═══════════════════════════════════════════════════════");
    Console.WriteLine();
    Console.WriteLine("HTTP Results:");
    Console.WriteLine($"  Total Requests:   {result.HttpStats.TotalRequests}");
    Console.WriteLine($"  Successful:       {result.HttpStats.SuccessfulRequests}");
    Console.WriteLine($"  Failed:           {result.HttpStats.FailedRequests}");
    Console.WriteLine($"  Average Duration: {result.HttpStats.AverageDuration:F2} ms");
    Console.WriteLine($"  Min Duration:     {result.HttpStats.MinDuration:F2} ms");
    Console.WriteLine($"  Max Duration:     {result.HttpStats.MaxDuration:F2} ms");
    Console.WriteLine($"  Total Time:       {result.HttpStats.TotalDuration:F2} ms");
    Console.WriteLine();
    Console.WriteLine("gRPC Results:");
    Console.WriteLine($"  Total Requests:   {result.GrpcStats.TotalRequests}");
    Console.WriteLine($"  Successful:       {result.GrpcStats.SuccessfulRequests}");
    Console.WriteLine($"  Failed:           {result.GrpcStats.FailedRequests}");
    Console.WriteLine($"  Average Duration: {result.GrpcStats.AverageDuration:F2} ms");
    Console.WriteLine($"  Min Duration:     {result.GrpcStats.MinDuration:F2} ms");
    Console.WriteLine($"  Max Duration:     {result.GrpcStats.MaxDuration:F2} ms");
    Console.WriteLine($"  Total Time:       {result.GrpcStats.TotalDuration:F2} ms");
    Console.WriteLine();
    Console.WriteLine($"Performance Difference: {result.PerformanceGainPercentage:F2}%");
    Console.WriteLine($"Winner: {(result.HttpStats.AverageDuration < result.GrpcStats.AverageDuration ? "HTTP ⚡" : "gRPC ⚡")}");
    Console.WriteLine("═══════════════════════════════════════════════════════");
}

static void DisplayConcurrencyResults(ConcurrencyResult result, string testType)
{
    Console.WriteLine("═══════════════════════════════════════════════════════");
    Console.WriteLine($"📊 {testType.ToUpper()} TEST RESULTS");
    Console.WriteLine("═══════════════════════════════════════════════════════");
    Console.WriteLine($"HTTP Total Duration:  {result.HttpTotalDuration:F2} ms");
    Console.WriteLine($"gRPC Total Duration:  {result.GrpcTotalDuration:F2} ms");
    Console.WriteLine($"HTTP Success Rate:    {result.HttpSuccessRate:F2}%");
    Console.WriteLine($"gRPC Success Rate:    {result.GrpcSuccessRate:F2}%");
    Console.WriteLine($"Performance Gain:     {result.PerformanceGainPercentage:F2}%");
    Console.WriteLine($"Winner:               {(result.HttpTotalDuration < result.GrpcTotalDuration ? "HTTP ⚡" : "gRPC ⚡")}");
    Console.WriteLine("═══════════════════════════════════════════════════════");
}