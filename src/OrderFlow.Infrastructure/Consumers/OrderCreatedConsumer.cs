using MassTransit;
using Microsoft.Extensions.Logging;
using OrderFlow.Domain.Common;
using OrderFlow.Domain.Inventories;
using OrderFlow.Domain.Orders;
using OrderFlow.Domain.Orders.Events;
using OrderFlow.Infrastructure.Payment;

namespace OrderFlow.Infrastructure.Consumers;

public sealed class OrderCreatedConsumer : IConsumer<OrderCreatedDomainEvent>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderCreatedConsumer> _logger;

    public OrderCreatedConsumer(
        IOrderRepository orderRepository,
        IInventoryRepository inventoryRepository,
        IPaymentGateway paymentGateway,
        IUnitOfWork unitOfWork,
        ILogger<OrderCreatedConsumer> logger)
    {
        _orderRepository = orderRepository;
        _inventoryRepository = inventoryRepository;
        _paymentGateway = paymentGateway;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreatedDomainEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Received OrderCreatedDomainEvent for order {OrderId}", message.OrderId.Value);

        var order = await _orderRepository.GetByIdAsync(message.OrderId, context.CancellationToken);
        if (order is null)
        {
            _logger.LogWarning("Order {OrderId} not found for payment processing.", message.OrderId.Value);
            return;
        }

        // Process payment with resilient Polly pipeline
        var paymentResult = await _paymentGateway.ProcessPaymentAsync(
            order.Id,
            order.TotalAmount,
            context.CancellationToken);

        if (paymentResult.IsSuccess)
        {
            _logger.LogInformation("Payment succeeded for order {OrderId}. Marking as Paid.", order.Id.Value);
            order.MarkAsPaid(DateTime.UtcNow);

            // Deduct stock permanently from inventory
            foreach (var item in order.Items)
            {
                var inventory = await _inventoryRepository.GetByProductIdAsync(item.ProductId, context.CancellationToken);
                inventory?.ConfirmDeduction(item.Quantity);
            }
        }
        else
        {
            _logger.LogWarning("Payment failed for order {OrderId}: {Error}. Compensating order...",
                order.Id.Value, paymentResult.Error.Description);

            order.Cancel($"Payment failed: {paymentResult.Error.Description}", DateTime.UtcNow);

            // Release reserved stock back to inventory (Saga Compensation)
            foreach (var item in order.Items)
            {
                var inventory = await _inventoryRepository.GetByProductIdAsync(item.ProductId, context.CancellationToken);
                inventory?.ReleaseReservation(item.Quantity);
            }
        }

        await _unitOfWork.SaveChangesAsync(context.CancellationToken);
    }
}
