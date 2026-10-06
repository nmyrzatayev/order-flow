using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderFlow.Infrastructure.Outbox;

namespace OrderFlow.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Type)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(m => m.Content)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(m => m.OccurredOnUtc)
            .IsRequired();

        builder.HasIndex(m => m.ProcessedOnUtc);
    }
}
