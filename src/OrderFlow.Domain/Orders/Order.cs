using OrderFlow.Domain.Common;
using OrderFlow.Domain.Inventories;
using OrderFlow.Domain.Orders.Events;
using OrderFlow.Domain.Shared;

namespace OrderFlow.Domain.Orders;

public sealed class Order : AggregateRoot<OrderId>
{
    private readonly List<OrderItem> _items = [];

    public CustomerId CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? PaidAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public string? CancellationReason { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public Money TotalAmount => _items.Count == 0
        ? Money.Zero(Currency.Usd)
        : _items.Select(i => i.TotalPrice).Aggregate((a, b) => a + b);

    private Order(OrderId id, CustomerId customerId, DateTime createdAtUtc)
        : base(id)
    {
        CustomerId = customerId;
        Status = OrderStatus.Pending;
        CreatedAtUtc = createdAtUtc;
    }

    // Parameterless constructor for EF Core
    private Order() { }

    public static Result<Order> Create(
        CustomerId customerId,
        IEnumerable<(ProductId ProductId, int Quantity, Money UnitPrice)> lineItems)
    {
        var itemsList = lineItems.ToList();
        if (itemsList.Count == 0)
        {
            return Result.Failure<Order>(
                Error.Validation("Order.EmptyItems", "An order must contain at least one item."));
        }

        var firstCurrency = itemsList[0].UnitPrice.Currency;
        if (itemsList.Any(item => item.UnitPrice.Currency != firstCurrency))
        {
            return Result.Failure<Order>(
                Error.Validation("Order.MixedCurrencies", "All items in an order must use the same currency."));
        }

        var orderId = OrderId.New();
        var order = new Order(orderId, customerId, DateTime.UtcNow);

        foreach (var (productId, quantity, unitPrice) in itemsList)
        {
            var itemResult = OrderItem.Create(orderId, productId, quantity, unitPrice);
            if (itemResult.IsFailure)
                return Result.Failure<Order>(itemResult.Error);

            order._items.Add(itemResult.Value);
        }

        order.RaiseDomainEvent(new OrderCreatedDomainEvent(order.Id, order.CustomerId, order.TotalAmount));

        return order;
    }

    public Result MarkAsPaid(DateTime paidAtUtc)
    {
        if (Status == OrderStatus.Paid)
        {
            return Result.Failure(
                Error.Conflict("Order.AlreadyPaid", "Order is already paid."));
        }

        if (Status == OrderStatus.Cancelled)
        {
            return Result.Failure(
                Error.Conflict("Order.Cancelled", "Cannot mark a cancelled order as paid."));
        }

        Status = OrderStatus.Paid;
        PaidAtUtc = paidAtUtc;

        RaiseDomainEvent(new OrderPaidDomainEvent(Id, paidAtUtc));

        return Result.Success();
    }

    public Result Cancel(string reason, DateTime cancelledAtUtc)
    {
        if (Status == OrderStatus.Cancelled)
        {
            return Result.Failure(
                Error.Conflict("Order.AlreadyCancelled", "Order is already cancelled."));
        }

        if (Status == OrderStatus.Completed)
        {
            return Result.Failure(
                Error.Conflict("Order.Completed", "Cannot cancel an already completed order."));
        }

        Status = OrderStatus.Cancelled;
        CancelledAtUtc = cancelledAtUtc;
        CancellationReason = reason;

        RaiseDomainEvent(new OrderCancelledDomainEvent(Id, reason, cancelledAtUtc));

        return Result.Success();
    }
}
