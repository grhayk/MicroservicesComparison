namespace PerformanceTests.Models
{
    public class ConcurrencyResult
    {
        public double HttpTotalDuration { get; set; }
        public double GrpcTotalDuration { get; set; }
        public double HttpSuccessRate { get; set; }
        public double GrpcSuccessRate { get; set; }
        public double PerformanceGainPercentage { get; set; }
    }
}
