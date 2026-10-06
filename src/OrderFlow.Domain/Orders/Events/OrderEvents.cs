using OrderFlow.Domain.Common;
using OrderFlow.Domain.Shared;

namespace OrderFlow.Domain.Orders.Events;

public sealed record OrderCreatedDomainEvent(
    OrderId OrderId,
    CustomerId CustomerId,
    Money TotalAmount) : DomainEvent;

public sealed record OrderPaidDomainEvent(
    OrderId OrderId,
    DateTime PaidAtUtc) : DomainEvent;

public sealed record OrderCancelledDomainEvent(
    OrderId OrderId,
    string Reason,
    DateTime CancelledAtUtc) : DomainEvent;
