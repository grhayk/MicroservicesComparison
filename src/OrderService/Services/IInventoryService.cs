namespace OrderService.Services
{
    public interface IInventoryService
    {
        Task<(bool IsAvailable, int AvailableQuantity, double DurationMs)> CheckAvailabilityAsync(
            string productId, int quantity);

        Task<(bool Success, string Message, double DurationMs)> ReserveItemsAsync(
            string productId, int quantity, string orderId);

        Task<(bool Success, double DurationMs)> ReleaseItemsAsync(
            string productId, int quantity, string orderId);
    }
}
