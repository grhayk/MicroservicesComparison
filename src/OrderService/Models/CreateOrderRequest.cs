namespace OrderService.Models
{
    public class CreateOrderRequest
    {
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public List<OrderItemRequest> Items { get; set; } = new();
    }
}
