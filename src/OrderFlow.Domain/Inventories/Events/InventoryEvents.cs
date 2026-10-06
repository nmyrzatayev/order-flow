using OrderFlow.Domain.Common;

namespace OrderFlow.Domain.Inventories.Events;

public sealed record InventoryReservedDomainEvent(
    InventoryId InventoryId,
    ProductId ProductId,
    int Quantity) : DomainEvent;

public sealed record InventoryReservationReleasedDomainEvent(
    InventoryId InventoryId,
    ProductId ProductId,
    int Quantity) : DomainEvent;
