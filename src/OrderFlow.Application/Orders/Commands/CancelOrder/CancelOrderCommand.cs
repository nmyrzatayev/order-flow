using MediatR;
using OrderFlow.Domain.Common;

namespace OrderFlow.Application.Orders.Commands.CancelOrder;

public sealed record CancelOrderCommand(Guid OrderId, string Reason) : IRequest<Result>;
