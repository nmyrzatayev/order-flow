using MediatR;
using OrderFlow.Domain.Common;
using OrderFlow.Domain.Inventories;
using OrderFlow.Domain.Orders;

namespace OrderFlow.Application.Orders.Commands.CancelOrder;

public sealed class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, Result>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelOrderCommandHandler(
        IOrderRepository orderRepository,
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var orderId = new OrderId(request.OrderId);
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);

        if (order is null)
        {
            return Result.Failure(
                Error.NotFound("Order.NotFound", $"Order with ID {request.OrderId} was not found."));
        }

        var cancelResult = order.Cancel(request.Reason, DateTime.UtcNow);
        if (cancelResult.IsFailure)
        {
            return cancelResult;
        }

        // Release reserved stock for each item
        foreach (var item in order.Items)
        {
            var inventory = await _inventoryRepository.GetByProductIdAsync(item.ProductId, cancellationToken);
            if (inventory is not null)
            {
                inventory.ReleaseReservation(item.Quantity);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
