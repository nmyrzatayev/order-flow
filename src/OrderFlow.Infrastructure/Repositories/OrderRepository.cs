using Microsoft.EntityFrameworkCore;
using OrderFlow.Domain.Orders;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Infrastructure.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly OrderFlowDbContext _dbContext;

    public OrderRepository(OrderFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Order?> GetByIdAsync(OrderId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await _dbContext.Orders.AddAsync(order, cancellationToken);
    }

    public void Update(Order order)
    {
        _dbContext.Orders.Update(order);
    }
}
