using System.Text.Json;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OrderFlow.Domain.Common;
using OrderFlow.Infrastructure.Outbox;

namespace OrderFlow.Infrastructure.Persistence.Interceptors;

public sealed class InsertOutboxMessagesInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false
    };

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var dbContext = eventData.Context;
        if (dbContext is null)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var domainEvents = dbContext.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Select(entry => entry.Entity)
            .SelectMany(aggregate =>
            {
                var events = aggregate.DomainEvents.ToList();
                aggregate.ClearDomainEvents();
                return events;
            })
            .ToList();

        if (domainEvents.Count == 0)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var outboxMessages = domainEvents.Select(domainEvent => new OutboxMessage
        {
            Id = domainEvent.EventId,
            OccurredOnUtc = domainEvent.OccurredOnUtc,
            Type = domainEvent.GetType().AssemblyQualifiedName ?? domainEvent.GetType().FullName!,
            Content = JsonSerializer.Serialize((object)domainEvent, domainEvent.GetType(), SerializerOptions)
        }).ToList();

        dbContext.Set<OutboxMessage>().AddRange(outboxMessages);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
