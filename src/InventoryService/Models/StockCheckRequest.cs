namespace InventoryService.Models
{
    public class StockCheckRequest
    {
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
