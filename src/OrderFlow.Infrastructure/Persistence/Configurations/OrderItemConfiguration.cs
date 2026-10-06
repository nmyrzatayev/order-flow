using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderFlow.Domain.Inventories;
using OrderFlow.Domain.Orders;
using OrderFlow.Domain.Shared;

namespace OrderFlow.Infrastructure.Persistence.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.OrderId)
            .HasConversion(
                id => id.Value,
                value => new OrderId(value))
            .IsRequired();

        builder.Property(i => i.ProductId)
            .HasConversion(
                id => id.Value,
                value => new ProductId(value))
            .IsRequired();

        builder.Property(i => i.Quantity)
            .IsRequired();

        builder.OwnsOne(i => i.UnitPrice, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount)
                .HasColumnName("unit_price_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            moneyBuilder.Property(m => m.Currency)
                .HasColumnName("unit_price_currency")
                .HasConversion(
                    c => c.Code,
                    code => Currency.FromCode(code).Value)
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Ignore(i => i.TotalPrice);
    }
}
