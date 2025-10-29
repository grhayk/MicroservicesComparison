namespace OrderService.Models
{
    public class CreateOrderResponse
    {
        public bool Success { get; set; }
        public string? OrderId { get; set; }
        public string Message { get; set; } = string.Empty;
        public Order? Order { get; set; }
        public PerformanceMetrics? Metrics { get; set; }
    }
}
