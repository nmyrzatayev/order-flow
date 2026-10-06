using OrderFlow.Domain.Common;
using OrderFlow.Domain.Orders;
using OrderFlow.Domain.Shared;

namespace OrderFlow.Infrastructure.Payment;

public interface IPaymentGateway
{
    Task<Result<Guid>> ProcessPaymentAsync(
        OrderId orderId,
        Money amount,
        CancellationToken cancellationToken = default);
}
