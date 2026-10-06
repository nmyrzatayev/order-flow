using OrderFlow.Domain.Common;
using OrderFlow.Domain.Inventories;
using OrderFlow.Domain.Shared;

namespace OrderFlow.Domain.Orders;

public sealed class OrderItem : Entity<Guid>
{
    public OrderId OrderId { get; private set; }
    public ProductId ProductId { get; private set; }
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; }
    public Money TotalPrice => UnitPrice * Quantity;

    private OrderItem(Guid id, OrderId orderId, ProductId productId, int quantity, Money unitPrice)
        : base(id)
    {
        OrderId = orderId;
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    // Parameterless constructor for EF Core
    private OrderItem() { }

    internal static Result<OrderItem> Create(OrderId orderId, ProductId productId, int quantity, Money unitPrice)
    {
        if (quantity <= 0)
        {
            return Result.Failure<OrderItem>(
                Error.Validation("OrderItem.InvalidQuantity", "Quantity must be greater than zero."));
        }

        return new OrderItem(Guid.NewGuid(), orderId, productId, quantity, unitPrice);
    }
}
