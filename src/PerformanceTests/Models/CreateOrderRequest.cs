namespace PerformanceTests.Models
{
    public class CreateOrderRequest
    {
        public string CustomerId { get; set; } = "test-customer";
        public string CustomerEmail { get; set; } = "test@example.com";
        public List<OrderItemRequest> Items { get; set; } = new();
    }
}
