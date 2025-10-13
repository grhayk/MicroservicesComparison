using Grpc.Core;
using InventoryService.Data;
using InventoryService.Grpc;
namespace InventoryService.Services
{
    public class InventoryGrpcService : Grpc.InventoryService.InventoryServiceBase
    {
        private readonly IInventoryRepository _repository;
        private readonly ILogger<InventoryGrpcService> _logger;

        public InventoryGrpcService(IInventoryRepository repository, ILogger<InventoryGrpcService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public override async Task<CheckAvailabilityResponse> CheckAvailability(
            CheckAvailabilityRequest request,
            ServerCallContext context)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                var isAvailable = await _repository.CheckAvailabilityAsync(request.ProductId, request.Quantity);
                var availableQuantity = await _repository.GetAvailableQuantityAsync(request.ProductId);

                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

                var response = new CheckAvailabilityResponse
                {
                    IsAvailable = isAvailable,
                    AvailableQuantity = availableQuantity,
                    Message = isAvailable
                        ? $"{request.Quantity} units available"
                        : $"Insufficient stock. Only {availableQuantity} available"
                };

                _logger.LogInformation("gRPC: CheckAvailability for {ProductId} (quantity: {Quantity}) completed in {Duration}ms - Result: {IsAvailable}",
                    request.ProductId, request.Quantity, duration, isAvailable);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "gRPC: Error checking availability for {ProductId}", request.ProductId);
                throw new RpcException(new Status(StatusCode.Internal, "Error checking availability"));
            }
        }

        public override async Task<ReserveItemsResponse> ReserveItems(
            ReserveItemsRequest request,
            ServerCallContext context)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                var success = await _repository.ReserveItemsAsync(request.ProductId, request.Quantity);
                var remainingQuantity = await _repository.GetAvailableQuantityAsync(request.ProductId);

                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

                var response = new ReserveItemsResponse
                {
                    Success = success,
                    Message = success
                        ? $"Successfully reserved {request.Quantity} units for order {request.OrderId}"
                        : "Failed to reserve items - insufficient stock",
                    RemainingQuantity = remainingQuantity
                };

                _logger.LogInformation("gRPC: ReserveItems for {ProductId} (quantity: {Quantity}, orderId: {OrderId}) completed in {Duration}ms - Result: {Success}",
                    request.ProductId, request.Quantity, request.OrderId, duration, success);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "gRPC: Error reserving items for {ProductId}", request.ProductId);
                throw new RpcException(new Status(StatusCode.Internal, "Error reserving items"));
            }
        }

        public override async Task<ReleaseItemsResponse> ReleaseItems(
            ReleaseItemsRequest request,
            ServerCallContext context)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                var success = await _repository.ReleaseItemsAsync(request.ProductId, request.Quantity);
                var updatedQuantity = await _repository.GetAvailableQuantityAsync(request.ProductId);

                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

                var response = new ReleaseItemsResponse
                {
                    Success = success,
                    Message = success
                        ? $"Successfully released {request.Quantity} units"
                        : "Failed to release items",
                    UpdatedQuantity = updatedQuantity
                };

                _logger.LogInformation("gRPC: ReleaseItems for {ProductId} (quantity: {Quantity}) completed in {Duration}ms",
                    request.ProductId, request.Quantity, duration);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "gRPC: Error releasing items for {ProductId}", request.ProductId);
                throw new RpcException(new Status(StatusCode.Internal, "Error releasing items"));
            }
        }

        public override async Task<GetItemResponse> GetItem(
            GetItemRequest request,
            ServerCallContext context)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                var item = await _repository.GetItemAsync(request.ProductId);

                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

                if (item == null)
                {
                    _logger.LogWarning("gRPC: Product {ProductId} not found", request.ProductId);
                    return new GetItemResponse { Found = false };
                }

                var response = new GetItemResponse
                {
                    ProductId = item.ProductId,
                    Name = item.Name,
                    Quantity = item.Quantity,
                    Price = (double)item.Price,
                    Found = true
                };

                _logger.LogInformation("gRPC: GetItem for {ProductId} completed in {Duration}ms",
                    request.ProductId, duration);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "gRPC: Error getting item {ProductId}", request.ProductId);
                throw new RpcException(new Status(StatusCode.Internal, "Error getting item"));
            }
        }
    }
}
