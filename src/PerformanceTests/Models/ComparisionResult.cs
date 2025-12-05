namespace PerformanceTests.Models
{
    public class ComparisonResult
    {
        public double HttpDuration { get; set; }
        public double GrpcDuration { get; set; }
        public double PerformanceGainPercentage { get; set; }
        public bool HttpSuccess { get; set; }
        public bool GrpcSuccess { get; set; }
    }
}
