using Microsoft.EntityFrameworkCore;
using OrderFlow.Domain.Inventories;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Infrastructure.Repositories;

public sealed class InventoryRepository : IInventoryRepository
{
    private readonly OrderFlowDbContext _dbContext;

    public InventoryRepository(OrderFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Inventory?> GetByIdAsync(InventoryId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Inventories
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<Inventory?> GetByProductIdAsync(ProductId productId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Inventories
            .FirstOrDefaultAsync(i => i.ProductId == productId, cancellationToken);
    }

    public async Task AddAsync(Inventory inventory, CancellationToken cancellationToken = default)
    {
        await _dbContext.Inventories.AddAsync(inventory, cancellationToken);
    }

    public void Update(Inventory inventory)
    {
        _dbContext.Inventories.Update(inventory);
    }
}
