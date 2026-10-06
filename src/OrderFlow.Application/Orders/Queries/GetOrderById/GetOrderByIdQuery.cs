using MediatR;
using OrderFlow.Domain.Common;

namespace OrderFlow.Application.Orders.Queries.GetOrderById;

public sealed record GetOrderByIdQuery(Guid OrderId) : IRequest<Result<OrderResponse>>;

public interface IOrderQueries
{
    Task<OrderResponse?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default);
}
