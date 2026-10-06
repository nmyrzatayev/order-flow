using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Infrastructure.Outbox;

public sealed class OutboxProcessorBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessorBackgroundService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(2);

    public OutboxProcessorBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessorBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox processor background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while processing outbox messages.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var messages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Processing {Count} outbox messages...", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                var eventType = Type.GetType(message.Type);
                if (eventType is null)
                {
                    _logger.LogWarning("Unknown event type: {Type}", message.Type);
                    message.Error = $"Unknown event type: {message.Type}";
                    message.ProcessedOnUtc = DateTime.UtcNow;
                    continue;
                }

                var domainEvent = JsonSerializer.Deserialize(message.Content, eventType);
                if (domainEvent is null)
                {
                    message.Error = "Failed to deserialize event payload";
                    message.ProcessedOnUtc = DateTime.UtcNow;
                    continue;
                }

                await publishEndpoint.Publish(domainEvent, eventType, cancellationToken);

                message.ProcessedOnUtc = DateTime.UtcNow;
                _logger.LogInformation("Published outbox message {Id} of type {Type}", message.Id, message.Type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish outbox message {Id}", message.Id);
                message.Error = ex.Message;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
