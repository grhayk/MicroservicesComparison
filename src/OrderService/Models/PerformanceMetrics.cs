namespace OrderService.Models
{
    public class PerformanceMetrics
    {
        public double TotalDurationMs { get; set; }
        public double InventoryCheckDurationMs { get; set; }
        public double InventoryReserveDurationMs { get; set; }
        public double NotificationDurationMs { get; set; }
        public string CommunicationType { get; set; } = string.Empty; // "HTTP" or "gRPC"
    }
}
