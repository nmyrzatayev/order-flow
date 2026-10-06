using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OrderFlow.Api.Endpoints;
using OrderFlow.Api.Middleware;
using OrderFlow.Application;
using OrderFlow.Infrastructure;
using OrderFlow.Infrastructure.Persistence;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{TraceId}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Add Layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

#region Подключение телеметрии
// OpenTelemetry Configuration (Tracing & Metrics)
const string serviceName = "OrderFlow.Service";
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddNpgsql();

        var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            tracing.AddOtlpExporter(opt => opt.Endpoint = new Uri(otlpEndpoint));
        }
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();

        var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            metrics.AddOtlpExporter(opt => opt.Endpoint = new Uri(otlpEndpoint));
        }
    });

// OpenAPI & Scalar
builder.Services.AddOpenApi();

#endregion

var app = builder.Build();

// Ensure Database is initialized
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

// Global Exception Handler
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// Idempotency Middleware
app.UseMiddleware<IdempotencyMiddleware>();

// Scalar OpenAPI UI
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Map Endpoints
app.MapOrderEndpoints();
app.MapInventoryEndpoints();

app.Run();

// Needed for WebApplicationFactory in IntegrationTests
public partial class Program { }
