namespace InventoryService.Models
{
    public class ReserveStockResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int RemainingQuantity { get; set; }
    }
}
