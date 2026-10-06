using MediatR;
using OrderFlow.Domain.Common;
using OrderFlow.Domain.Inventories;
using OrderFlow.Domain.Orders;
using OrderFlow.Domain.Shared;

namespace OrderFlow.Application.Orders.Commands.CreateOrder;

public sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<Guid>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var lineItems = new List<(ProductId ProductId, int Quantity, Money UnitPrice)>();

        foreach (var itemDto in request.Items)
        {
            var currencyResult = Currency.FromCode(itemDto.Currency);
            if (currencyResult.IsFailure)
                return Result.Failure<Guid>(currencyResult.Error);

            var moneyResult = Money.Create(itemDto.UnitPrice, currencyResult.Value);
            if (moneyResult.IsFailure)
                return Result.Failure<Guid>(moneyResult.Error);

            var productId = new ProductId(itemDto.ProductId);

            // Check & Reserve inventory
            var inventory = await _inventoryRepository.GetByProductIdAsync(productId, cancellationToken);
            if (inventory is null)
            {
                return Result.Failure<Guid>(
                    Error.NotFound("Inventory.NotFound", $"Inventory for product {itemDto.ProductId} was not found."));
            }

            var reserveResult = inventory.Reserve(itemDto.Quantity);
            if (reserveResult.IsFailure)
                return Result.Failure<Guid>(reserveResult.Error);

            lineItems.Add((productId, itemDto.Quantity, moneyResult.Value));
        }

        var customerId = new CustomerId(request.CustomerId);
        var orderResult = Order.Create(customerId, lineItems);

        if (orderResult.IsFailure)
            return Result.Failure<Guid>(orderResult.Error);

        var order = orderResult.Value;

        await _orderRepository.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(order.Id.Value);
    }
}
