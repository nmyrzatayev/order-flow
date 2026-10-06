using MediatR;
using OrderFlow.Domain.Common;

namespace OrderFlow.Application.Orders.Queries.GetOrderById;

public sealed class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, Result<OrderResponse>>
{
    private readonly IOrderQueries _orderQueries;

    public GetOrderByIdQueryHandler(IOrderQueries orderQueries)
    {
        _orderQueries = orderQueries;
    }

    public async Task<Result<OrderResponse>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderQueries.GetOrderByIdAsync(request.OrderId, cancellationToken);

        if (order is null)
        {
            return Result.Failure<OrderResponse>(
                Error.NotFound("Order.NotFound", $"Order with ID {request.OrderId} was not found."));
        }

        return Result.Success(order);
    }
}
