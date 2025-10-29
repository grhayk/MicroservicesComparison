using OrderService.Models;
using RabbitMQ.Client;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace OrderService.MessageBroker
{
    public interface IRabbitMqPublisher
    {
        Task<double> PublishOrderNotificationAsync(NotificationMessage message);
    }

    public class RabbitMqPublisher : IRabbitMqPublisher, IDisposable
    {
        private readonly ILogger<RabbitMqPublisher> _logger;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private const string ExchangeName = "notifications";
        private const string QueueName = "order-notifications";
        private const string RoutingKey = "order.created";

        public RabbitMqPublisher(IConfiguration configuration, ILogger<RabbitMqPublisher> logger)
        {
            _logger = logger;

            var factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMQ:Host"] ?? "localhost",
                Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = configuration["RabbitMQ:Username"] ?? "guest",
                Password = configuration["RabbitMQ:Password"] ?? "guest",
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            try
            {
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                // Declare exchange
                _channel.ExchangeDeclare(
                    exchange: ExchangeName,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false);

                // Declare queue
                _channel.QueueDeclare(
                    queue: QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                // Bind queue to exchange
                _channel.QueueBind(
                    queue: QueueName,
                    exchange: ExchangeName,
                    routingKey: RoutingKey);

                _logger.LogInformation("RabbitMQ publisher initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize RabbitMQ publisher");
                throw;
            }
        }

        public Task<double> PublishOrderNotificationAsync(NotificationMessage message)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var json = JsonSerializer.Serialize(message);
                var body = Encoding.UTF8.GetBytes(json);

                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.ContentType = "application/json";
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                _channel.BasicPublish(
                    exchange: ExchangeName,
                    routingKey: RoutingKey,
                    basicProperties: properties,
                    body: body);

                stopwatch.Stop();

                _logger.LogInformation("RabbitMQ: Published notification message in {Duration}ms - Type: {Type}, Recipient: {Recipient}",
                    stopwatch.Elapsed.TotalMilliseconds, message.Type, message.RecipientEmail);

                return Task.FromResult(stopwatch.Elapsed.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "RabbitMQ: Failed to publish notification message");
                return Task.FromResult(stopwatch.Elapsed.TotalMilliseconds);
            }
        }

        public void Dispose()
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
            _logger.LogInformation("RabbitMQ publisher disposed");
        }
    }
}
