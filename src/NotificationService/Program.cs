using NotificationService.Services;
using NotificationService.Workers;

var builder = Host.CreateApplicationBuilder(args);

// Register services
builder.Services.AddSingleton<IEmailService, EmailService>();

// Register the RabbitMQ consumer as a hosted service
builder.Services.AddHostedService<RabbitMqConsumer>();

var host = builder.Build();

// Log startup information
var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("═══════════════════════════════════════════════════════");
logger.LogInformation("🔔 Notification Service Starting");
logger.LogInformation("═══════════════════════════════════════════════════════");
logger.LogInformation("Service Type: Background Worker");
logger.LogInformation("Message Broker: RabbitMQ");
logger.LogInformation("Communication: Asynchronous (Event-Driven)");
logger.LogInformation("Purpose: Process order notifications from queue");
logger.LogInformation("═══════════════════════════════════════════════════════");

await host.RunAsync();
