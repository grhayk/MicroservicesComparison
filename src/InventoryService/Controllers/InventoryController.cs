using InventoryService.Data;
using InventoryService.Models;
using Microsoft.AspNetCore.Mvc;

namespace InventoryService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryRepository _repository;
        private readonly ILogger<InventoryController> _logger;

        public InventoryController(IInventoryRepository repository, ILogger<InventoryController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        /// <summary>
        /// Get all inventory items
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<InventoryItem>>> GetAll()
        {
            var startTime = DateTime.UtcNow;
            var items = await _repository.GetAllItemsAsync();
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

            _logger.LogInformation("REST: GetAll completed in {Duration}ms", duration);

            return Ok(items);
        }

        /// <summary>
        /// Get specific item by product ID
        /// </summary>
        [HttpGet("{productId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<InventoryItem>> GetItem(string productId)
        {
            var startTime = DateTime.UtcNow;
            var item = await _repository.GetItemAsync(productId);
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

            if (item == null)
            {
                _logger.LogWarning("REST: Product {ProductId} not found", productId);
                return NotFound(new { message = $"Product {productId} not found" });
            }

            _logger.LogInformation("REST: GetItem for {ProductId} completed in {Duration}ms", productId, duration);
            return Ok(item);
        }

        /// <summary>
        /// Check if items are available in stock (HTTP/REST endpoint)
        /// </summary>
        [HttpPost("check")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<StockCheckResponse>> CheckAvailability([FromBody] StockCheckRequest request)
        {
            var startTime = DateTime.UtcNow;

            if (string.IsNullOrEmpty(request.ProductId) || request.Quantity <= 0)
            {
                return BadRequest(new { message = "Invalid request parameters" });
            }

            var isAvailable = await _repository.CheckAvailabilityAsync(request.ProductId, request.Quantity);
            var availableQuantity = await _repository.GetAvailableQuantityAsync(request.ProductId);

            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

            var response = new StockCheckResponse
            {
                IsAvailable = isAvailable,
                AvailableQuantity = availableQuantity,
                Message = isAvailable
                    ? $"{request.Quantity} units available"
                    : $"Insufficient stock. Only {availableQuantity} available"
            };

            _logger.LogInformation("REST: CheckAvailability for {ProductId} (quantity: {Quantity}) completed in {Duration}ms - Result: {IsAvailable}",
                request.ProductId, request.Quantity, duration, isAvailable);

            return Ok(response);
        }

        /// <summary>
        /// Reserve items in stock (HTTP/REST endpoint)
        /// </summary>
        [HttpPost("reserve")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ReserveStockResponse>> ReserveItems([FromBody] ReserveStockRequest request)
        {
            var startTime = DateTime.UtcNow;

            if (string.IsNullOrEmpty(request.ProductId) || request.Quantity <= 0)
            {
                return BadRequest(new { message = "Invalid request parameters" });
            }

            var success = await _repository.ReserveItemsAsync(request.ProductId, request.Quantity);
            var remainingQuantity = await _repository.GetAvailableQuantityAsync(request.ProductId);

            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

            var response = new ReserveStockResponse
            {
                Success = success,
                Message = success
                    ? $"Successfully reserved {request.Quantity} units"
                    : "Failed to reserve items - insufficient stock",
                RemainingQuantity = remainingQuantity
            };

            _logger.LogInformation("REST: ReserveItems for {ProductId} (quantity: {Quantity}, orderId: {OrderId}) completed in {Duration}ms - Result: {Success}",
                request.ProductId, request.Quantity, request.OrderId, duration, success);

            return Ok(response);
        }

        /// <summary>
        /// Release previously reserved items (HTTP/REST endpoint)
        /// </summary>
        [HttpPost("release")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ReserveStockResponse>> ReleaseItems([FromBody] ReserveStockRequest request)
        {
            var startTime = DateTime.UtcNow;

            if (string.IsNullOrEmpty(request.ProductId) || request.Quantity <= 0)
            {
                return BadRequest(new { message = "Invalid request parameters" });
            }

            var success = await _repository.ReleaseItemsAsync(request.ProductId, request.Quantity);
            var remainingQuantity = await _repository.GetAvailableQuantityAsync(request.ProductId);

            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

            var response = new ReserveStockResponse
            {
                Success = success,
                Message = success
                    ? $"Successfully released {request.Quantity} units"
                    : "Failed to release items",
                RemainingQuantity = remainingQuantity
            };

            _logger.LogInformation("REST: ReleaseItems for {ProductId} (quantity: {Quantity}) completed in {Duration}ms",
                request.ProductId, request.Quantity, duration);

            return Ok(response);
        }
    }
}
