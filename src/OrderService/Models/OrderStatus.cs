namespace OrderService.Models
{
    public enum OrderStatus
    {
        Pending,
        ValidatingInventory,
        InventoryReserved,
        PaymentProcessing,
        Completed,
        Failed,
        Cancelled
    }
}
