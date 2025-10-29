using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure HTTP Client with Polly retry policy
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

// Register HTTP Inventory Service
builder.Services.AddHttpClient<HttpInventoryService>()
    .AddPolicyHandler(retryPolicy);

// Register gRPC Inventory Service
builder.Services.AddSingleton<GrpcInventoryService>();

// Register RabbitMQ Publisher
builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); // <-- This requires the Swashbuckle.AspNetCore NuGet package
    app.UseSwaggerUI();
}

app.UseCors();

app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapHealthChecks("/health");

// Root endpoint with info
app.MapGet("/", () => new
{
    service = "Order Service",
    version = "1.0.0",
    endpoints = new
    {
        createOrderHttp = "POST /api/orders/http",
        createOrderGrpc = "POST /api/orders/grpc",
        health = "/health",
        swagger = "/swagger"
    },
    communicationTypes = new[]
    {
        "HTTP/REST - Synchronous communication with Inventory Service",
        "gRPC - High-performance synchronous communication with Inventory Service",
        "RabbitMQ - Asynchronous messaging for notifications"
    },
    timestamp = DateTime.UtcNow
});

app.Logger.LogInformation("Order Service started");
app.Logger.LogInformation("HTTP communication available at: POST /api/orders/http");
app.Logger.LogInformation("gRPC communication available at: POST /api/orders/grpc");

app.Run();
