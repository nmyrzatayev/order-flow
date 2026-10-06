using Microsoft.EntityFrameworkCore;
using OrderFlow.Domain.Common;
using OrderFlow.Domain.Inventories;
using OrderFlow.Domain.Orders;
using OrderFlow.Infrastructure.Outbox;

namespace OrderFlow.Infrastructure.Persistence;

public class OrderFlowDbContext : DbContext, IUnitOfWork
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public OrderFlowDbContext(DbContextOptions<OrderFlowDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderFlowDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
