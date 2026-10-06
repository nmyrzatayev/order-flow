using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderFlow.Domain.Inventories;

namespace OrderFlow.Infrastructure.Persistence.Configurations;

public class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        builder.ToTable("inventories");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .HasConversion(
                id => id.Value,
                value => new InventoryId(value));

        builder.Property(i => i.ProductId)
            .HasConversion(
                id => id.Value,
                value => new ProductId(value))
            .IsRequired();

        builder.HasIndex(i => i.ProductId)
            .IsUnique();

        builder.Property(i => i.QuantityAvailable)
            .IsRequired();

        builder.Property(i => i.QuantityReserved)
            .IsRequired();

        // Optimistic Concurrency Token
        builder.Property(i => i.Version)
            .IsConcurrencyToken();

        builder.Ignore(i => i.DomainEvents);
    }
}
