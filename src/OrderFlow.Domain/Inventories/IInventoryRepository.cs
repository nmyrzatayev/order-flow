namespace OrderFlow.Domain.Inventories;

public interface IInventoryRepository
{
    Task<Inventory?> GetByIdAsync(InventoryId id, CancellationToken cancellationToken = default);
    Task<Inventory?> GetByProductIdAsync(ProductId productId, CancellationToken cancellationToken = default);
    Task AddAsync(Inventory inventory, CancellationToken cancellationToken = default);
    void Update(Inventory inventory);
}
