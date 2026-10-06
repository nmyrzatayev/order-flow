namespace OrderFlow.Application.Orders.Queries.GetOrderById;

public sealed record OrderItemResponse(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice,
    string Currency,
    decimal TotalPrice);

public sealed record OrderResponse(
    Guid Id,
    Guid CustomerId,
    string Status,
    decimal TotalAmount,
    string Currency,
    DateTime CreatedAtUtc,
    DateTime? PaidAtUtc,
    List<OrderItemResponse> Items);
