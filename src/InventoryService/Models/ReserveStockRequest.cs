namespace InventoryService.Models
{
    public class ReserveStockRequest
    {
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string OrderId { get; set; } = string.Empty;
    }
}
