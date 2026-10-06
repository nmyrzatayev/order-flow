using MediatR;
using OrderFlow.Domain.Common;

namespace OrderFlow.Application.Orders.Commands.CreateOrder;

public sealed record CreateOrderItemDto(Guid ProductId, int Quantity, decimal UnitPrice, string Currency);

public sealed record CreateOrderCommand(
    Guid CustomerId,
    List<CreateOrderItemDto> Items) : IRequest<Result<Guid>>;
