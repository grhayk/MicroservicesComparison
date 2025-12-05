namespace PerformanceTests.Models
{
    public class LoadTestResult
    {
        public TestStatistics HttpStats { get; set; } = new();
        public TestStatistics GrpcStats { get; set; } = new();
        public double PerformanceGainPercentage { get; set; }
    }
}
