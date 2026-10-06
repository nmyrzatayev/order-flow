using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Application.Orders.Queries.GetOrderById;
using OrderFlow.Domain.Common;
using OrderFlow.Domain.Inventories;
using OrderFlow.Domain.Orders;
using OrderFlow.Infrastructure.Consumers;
using OrderFlow.Infrastructure.Outbox;
using OrderFlow.Infrastructure.Payment;
using OrderFlow.Infrastructure.Persistence;
using OrderFlow.Infrastructure.Persistence.Interceptors;
using OrderFlow.Infrastructure.Queries;

namespace OrderFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? "Host=localhost;Port=5432;Database=orderflow;Username=postgres;Password=postgres";

        // Interceptors
        services.AddSingleton<InsertOutboxMessagesInterceptor>();

        // EF Core
        services.AddDbContext<OrderFlowDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<InsertOutboxMessagesInterceptor>();
            options.UseNpgsql(connectionString)
                   .AddInterceptors(interceptor);
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<OrderFlowDbContext>());

        // Repositories
        services.AddScoped<IOrderRepository, Repositories.OrderRepository>();
        services.AddScoped<IInventoryRepository, Repositories.InventoryRepository>();

        // HybridCache
#pragma warning disable EXTEXP0018 // HybridCache is in preview / experimental in some early toolings
        services.AddHybridCache();
#pragma warning restore EXTEXP0018

        // Dapper Queries
        services.AddScoped<IOrderQueries>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var activeConnectionString = config.GetConnectionString("Database")
                ?? "Host=localhost;Port=5432;Database=orderflow;Username=postgres;Password=postgres";
            var cache = sp.GetRequiredService<Microsoft.Extensions.Caching.Hybrid.HybridCache>();
            return new OrderQueries(activeConnectionString, cache);
        });

        // Payment Gateway
        services.AddSingleton<IPaymentGateway, ExternalPaymentGateway>();

        // Outbox background service
        services.AddHostedService<OutboxProcessorBackgroundService>();

        // MassTransit
        services.AddMassTransit(busConfig =>
        {
            busConfig.AddConsumer<OrderCreatedConsumer>();

            var rabbitMqHost = configuration["RabbitMQ:Host"];

            if (!string.IsNullOrWhiteSpace(rabbitMqHost))
            {
                busConfig.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(rabbitMqHost, "/", h =>
                    {
                        h.Username(configuration["RabbitMQ:Username"] ?? "guest");
                        h.Password(configuration["RabbitMQ:Password"] ?? "guest");
                    });

                    cfg.ConfigureEndpoints(context);
                });
            }
            else
            {
                // In-memory bus for local testing without external rabbitmq container
                busConfig.UsingInMemory((context, cfg) =>
                {
                    cfg.ConfigureEndpoints(context);
                });
            }
        });

        return services;
    }
}
