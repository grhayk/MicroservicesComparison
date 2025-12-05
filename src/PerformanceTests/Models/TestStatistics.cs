namespace PerformanceTests.Models
{
    public class TestStatistics
    {
        public int TotalRequests { get; set; }
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
        public double AverageDuration { get; set; }
        public double MinDuration { get; set; }
        public double MaxDuration { get; set; }
        public double TotalDuration { get; set; }
        public List<double> AllDurations { get; set; } = new();
    }
}
