using NotificationService.Models;
using NotificationService.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace NotificationService.Workers
{
    public class RabbitMqConsumer : BackgroundService
    {
        private readonly ILogger<RabbitMqConsumer> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private IConnection? _connection;
        private IModel? _channel;
        private const string ExchangeName = "notifications";
        private const string QueueName = "order-notifications";
        private const string RoutingKey = "order.created";
        private int _messageCount = 0;

        public RabbitMqConsumer(
            ILogger<RabbitMqConsumer> logger,
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notification Service - RabbitMQ Consumer starting...");

            // Wait a bit for RabbitMQ to be ready
            await Task.Delay(5000, stoppingToken);

            try
            {
                InitializeRabbitMq();

                var consumer = new EventingBasicConsumer(_channel);

                consumer.Received += async (model, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var messageJson = Encoding.UTF8.GetString(body);

                        _logger.LogInformation("Received message from RabbitMQ: {Message}", messageJson);

                        var message = JsonSerializer.Deserialize<NotificationMessage>(messageJson);

                        if (message != null)
                        {
                            using var scope = _serviceProvider.CreateScope();
                            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                            var result = await emailService.SendEmailAsync(message);

                            if (result.Success)
                            {
                                _messageCount++;
                                _logger.LogInformation(
                                    "✅ Message processed successfully (Total: {Count})\n" +
                                    "   Recipient: {Recipient}\n" +
                                    "   Type: {Type}\n" +
                                    "   Processing Time: {Duration}ms",
                                    _messageCount,
                                    message.RecipientEmail,
                                    message.Type,
                                    result.ProcessingTimeMs
                                );

                                // Acknowledge the message
                                _channel?.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                            }
                            else
                            {
                                _logger.LogError("❌ Failed to process message: {Message}", result.Message);

                                // Reject and requeue the message
                                _channel?.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing message from RabbitMQ");

                        // Reject and requeue on error
                        _channel?.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                    }
                };

                _channel?.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);

                _logger.LogInformation(
                    "🎧 RabbitMQ Consumer is now listening...\n" +
                    "   Exchange: {Exchange}\n" +
                    "   Queue: {Queue}\n" +
                    "   Routing Key: {RoutingKey}",
                    ExchangeName, QueueName, RoutingKey
                );

                // Keep the service running
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in RabbitMQ Consumer");
                throw;
            }
        }

        private void InitializeRabbitMq()
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
                Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = _configuration["RabbitMQ:Username"] ?? "guest",
                Password = _configuration["RabbitMQ:Password"] ?? "guest",
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

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

            // Set prefetch count (process one message at a time)
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            _logger.LogInformation("RabbitMQ connection established successfully");
        }

        public override void Dispose()
        {
            _logger.LogInformation("Shutting down RabbitMQ Consumer. Total messages processed: {Count}", _messageCount);

            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();

            base.Dispose();
        }
    }
}
