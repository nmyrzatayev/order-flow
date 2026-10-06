using OrderFlow.Domain.Common;
using OrderFlow.Domain.Inventories.Events;

namespace OrderFlow.Domain.Inventories;

public sealed class Inventory : AggregateRoot<InventoryId>
{
    public ProductId ProductId { get; private set; }
    public int QuantityAvailable { get; private set; }
    public int QuantityReserved { get; private set; }
    public uint Version { get; private set; } // Concurrency token

    private Inventory(InventoryId id, ProductId productId, int initialQuantity)
        : base(id)
    {
        ProductId = productId;
        QuantityAvailable = initialQuantity;
        QuantityReserved = 0;
    }

    // Parameterless constructor for EF Core
    private Inventory() { }

    public static Result<Inventory> Create(ProductId productId, int initialQuantity)
    {
        if (initialQuantity < 0)
        {
            return Result.Failure<Inventory>(
                Error.Validation("Inventory.InvalidQuantity", "Initial quantity cannot be negative."));
        }

        return new Inventory(InventoryId.New(), productId, initialQuantity);
    }

    public Result Reserve(int quantity)
    {
        if (quantity <= 0)
        {
            return Result.Failure(
                Error.Validation("Inventory.InvalidQuantity", "Reserved quantity must be greater than zero."));
        }

        if (QuantityAvailable < quantity)
        {
            return Result.Failure(
                Error.Conflict("Inventory.InsufficientStock",
                    $"Insufficient stock. Available: {QuantityAvailable}, Requested: {quantity}"));
        }

        QuantityAvailable -= quantity;
        QuantityReserved += quantity;

        RaiseDomainEvent(new InventoryReservedDomainEvent(Id, ProductId, quantity));

        return Result.Success();
    }

    public Result ReleaseReservation(int quantity)
    {
        if (quantity <= 0)
        {
            return Result.Failure(
                Error.Validation("Inventory.InvalidQuantity", "Released quantity must be greater than zero."));
        }

        if (QuantityReserved < quantity)
        {
            return Result.Failure(
                Error.Conflict("Inventory.ExcessiveRelease",
                    $"Cannot release more stock than reserved. Reserved: {QuantityReserved}, Requested: {quantity}"));
        }

        QuantityReserved -= quantity;
        QuantityAvailable += quantity;

        RaiseDomainEvent(new InventoryReservationReleasedDomainEvent(Id, ProductId, quantity));

        return Result.Success();
    }

    public Result ConfirmDeduction(int quantity)
    {
        if (quantity <= 0)
        {
            return Result.Failure(
                Error.Validation("Inventory.InvalidQuantity", "Deducted quantity must be greater than zero."));
        }

        if (QuantityReserved < quantity)
        {
            return Result.Failure(
                Error.Conflict("Inventory.ExcessiveDeduction",
                    $"Cannot deduct more stock than reserved. Reserved: {QuantityReserved}, Requested: {quantity}"));
        }

        QuantityReserved -= quantity;

        return Result.Success();
    }

    public Result Restock(int quantity)
    {
        if (quantity <= 0)
        {
            return Result.Failure(
                Error.Validation("Inventory.InvalidQuantity", "Restocked quantity must be greater than zero."));
        }

        QuantityAvailable += quantity;

        return Result.Success();
    }
}
