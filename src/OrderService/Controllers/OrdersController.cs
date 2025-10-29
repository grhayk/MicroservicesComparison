using Microsoft.AspNetCore.Mvc;
using OrderService.MessageBroker;
using OrderService.Models;
using OrderService.Services;
using System.Diagnostics;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly ILogger<OrdersController> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IRabbitMqPublisher _rabbitMqPublisher;

        public OrdersController(
            ILogger<OrdersController> logger,
            IServiceProvider serviceProvider,
            IRabbitMqPublisher rabbitMqPublisher)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _rabbitMqPublisher = rabbitMqPublisher;
        }

        /// <summary>
        /// Create order using HTTP communication
        /// </summary>
        [HttpPost("http")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CreateOrderResponse>> CreateOrderHttp(
            [FromBody] CreateOrderRequest request)
        {
            var inventoryService = _serviceProvider.GetRequiredService<HttpInventoryService>();
            return await ProcessOrder(request, inventoryService, "HTTP");
        }

        /// <summary>
        /// Create order using gRPC communication
        /// </summary>
        [HttpPost("grpc")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CreateOrderResponse>> CreateOrderGrpc(
            [FromBody] CreateOrderRequest request)
        {
            var inventoryService = _serviceProvider.GetRequiredService<GrpcInventoryService>();
            return await ProcessOrder(request, inventoryService, "gRPC");
        }

        private async Task<ActionResult<CreateOrderResponse>> ProcessOrder(
            CreateOrderRequest request,
            IInventoryService inventoryService,
            string communicationType)
        {
            var totalStopwatch = Stopwatch.StartNew();
            var orderId = Guid.NewGuid().ToString("N");

            _logger.LogInformation("Processing order {OrderId} using {CommunicationType} for customer {CustomerId}",
                orderId, communicationType, request.CustomerId);

            try
            {
                // Validate request
                if (!request.Items.Any())
                {
                    return BadRequest(new CreateOrderResponse
                    {
                        Success = false,
                        Message = "Order must contain at least one item"
                    });
                }

                var order = new Order
                {
                    OrderId = orderId,
                    CustomerId = request.CustomerId,
                    CustomerEmail = request.CustomerEmail,
                    Items = new List<OrderItem>(),
                    Status = OrderStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                var metrics = new PerformanceMetrics
                {
                    CommunicationType = communicationType
                };

                // Step 1: Check availability for all items
                order.Status = OrderStatus.ValidatingInventory;
                var checkStopwatch = Stopwatch.StartNew();

                foreach (var item in request.Items)
                {
                    var (isAvailable, availableQty, duration) = await inventoryService.CheckAvailabilityAsync(
                        item.ProductId, item.Quantity);

                    metrics.InventoryCheckDurationMs += duration;

                    if (!isAvailable)
                    {
                        _logger.LogWarning("Order {OrderId}: Insufficient stock for {ProductId}. Requested: {Requested}, Available: {Available}",
                            orderId, item.ProductId, item.Quantity, availableQty);

                        order.Status = OrderStatus.Failed;
                        order.ErrorMessage = $"Insufficient stock for product {item.ProductId}";

                        return Ok(new CreateOrderResponse
                        {
                            Success = false,
                            OrderId = orderId,
                            Message = order.ErrorMessage,
                            Order = order,
                            Metrics = metrics
                        });
                    }
                }

                checkStopwatch.Stop();
                _logger.LogInformation("Order {OrderId}: Inventory check completed in {Duration}ms",
                    orderId, checkStopwatch.Elapsed.TotalMilliseconds);

                // Step 2: Reserve items
                order.Status = OrderStatus.InventoryReserved;
                var reserveStopwatch = Stopwatch.StartNew();

                foreach (var item in request.Items)
                {
                    var (success, message, duration) = await inventoryService.ReserveItemsAsync(
                        item.ProductId, item.Quantity, orderId);

                    metrics.InventoryReserveDurationMs += duration;

                    if (!success)
                    {
                        _logger.LogError("Order {OrderId}: Failed to reserve {ProductId}: {Message}",
                            orderId, item.ProductId, message);

                        // Rollback previously reserved items
                        foreach (var reservedItem in order.Items)
                        {
                            await inventoryService.ReleaseItemsAsync(
                                reservedItem.ProductId, reservedItem.Quantity, orderId);
                        }

                        order.Status = OrderStatus.Failed;
                        order.ErrorMessage = $"Failed to reserve {item.ProductId}: {message}";

                        return Ok(new CreateOrderResponse
                        {
                            Success = false,
                            OrderId = orderId,
                            Message = order.ErrorMessage,
                            Order = order,
                            Metrics = metrics
                        });
                    }

                    // Add item to order (in real scenario, we'd fetch price from a product service)
                    order.Items.Add(new OrderItem
                    {
                        ProductId = item.ProductId,
                        ProductName = item.ProductId, // Simplified
                        Quantity = item.Quantity,
                        Price = 99.99m // Hardcoded for demo
                    });
                }

                reserveStopwatch.Stop();
                _logger.LogInformation("Order {OrderId}: Items reserved in {Duration}ms",
                    orderId, reserveStopwatch.Elapsed.TotalMilliseconds);

                // Calculate total
                order.TotalAmount = order.Items.Sum(i => i.Price * i.Quantity);

                // Step 3: Send notification via RabbitMQ (async)
                var notificationStopwatch = Stopwatch.StartNew();

                var notification = new NotificationMessage
                {
                    Type = "OrderConfirmation",
                    RecipientEmail = request.CustomerEmail,
                    Subject = $"Order Confirmation - {orderId}",
                    Body = $"Your order {orderId} has been confirmed. Total: ${order.TotalAmount:F2}",
                    Metadata = new Dictionary<string, string>
                    {
                        ["OrderId"] = orderId,
                        ["TotalAmount"] = order.TotalAmount.ToString("F2"),
                        ["ItemCount"] = order.Items.Count.ToString()
                    },
                    CreatedAt = DateTime.UtcNow
                };

                var notifDuration = await _rabbitMqPublisher.PublishOrderNotificationAsync(notification);
                metrics.NotificationDurationMs = notifDuration;

                notificationStopwatch.Stop();

                // Complete order
                order.Status = OrderStatus.Completed;
                order.CompletedAt = DateTime.UtcNow;

                totalStopwatch.Stop();
                metrics.TotalDurationMs = totalStopwatch.Elapsed.TotalMilliseconds;

                _logger.LogInformation("Order {OrderId} completed successfully in {Duration}ms using {CommunicationType}",
                    orderId, metrics.TotalDurationMs, communicationType);

                return Ok(new CreateOrderResponse
                {
                    Success = true,
                    OrderId = orderId,
                    Message = "Order created successfully",
                    Order = order,
                    Metrics = metrics
                });
            }
            catch (Exception ex)
            {
                totalStopwatch.Stop();
                _logger.LogError(ex, "Error processing order {OrderId}", orderId);

                return Ok(new CreateOrderResponse
                {
                    Success = false,
                    OrderId = orderId,
                    Message = $"Error processing order: {ex.Message}",
                    Metrics = new PerformanceMetrics
                    {
                        TotalDurationMs = totalStopwatch.Elapsed.TotalMilliseconds,
                        CommunicationType = communicationType
                    }
                });
            }
        }

        /// <summary>
        /// Health check endpoint
        /// </summary>
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "healthy", service = "OrderService", timestamp = DateTime.UtcNow });
        }
    }
}
