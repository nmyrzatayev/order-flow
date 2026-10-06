namespace OrderFlow.Domain.Inventories;

public readonly record struct ProductId(Guid Value)
{
    public static ProductId New() => new(Guid.NewGuid());
    public static ProductId Empty => new(Guid.Empty);

    public override string ToString() => Value.ToString();
}

public readonly record struct InventoryId(Guid Value)
{
    public static InventoryId New() => new(Guid.NewGuid());
    public static InventoryId Empty => new(Guid.Empty);

    public override string ToString() => Value.ToString();
}
