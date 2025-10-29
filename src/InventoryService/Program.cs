using InventoryService.Data;
using InventoryService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add gRPC
builder.Services.AddGrpc();

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod()
              .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding");
    });
});

// Register repository
builder.Services.AddSingleton<IInventoryRepository, InventoryRepository>();

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.UseAuthorization();

app.MapControllers();

// Map gRPC service
app.MapGrpcService<InventoryGrpcService>();

// Health check endpoint
app.MapHealthChecks("/health");

// Root endpoint with info
app.MapGet("/", () => new
{
    service = "Inventory Service",
    version = "1.0.0",
    endpoints = new
    {
        rest = "/api/inventory",
        grpc = "Port 8081",
        health = "/health",
        swagger = "/swagger"
    },
    timestamp = DateTime.UtcNow
});

app.Logger.LogInformation("Inventory Service started");
app.Logger.LogInformation("REST API available on port 8080");
app.Logger.LogInformation("gRPC service available on port 8081");

app.Run();
