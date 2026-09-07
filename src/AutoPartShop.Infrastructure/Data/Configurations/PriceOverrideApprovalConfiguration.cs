using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class PriceOverrideApprovalConfiguration : IEntityTypeConfiguration<PriceOverrideApproval>
{
    public void Configure(EntityTypeBuilder<PriceOverrideApproval> builder)
    {
        builder.ToTable("PriceOverrideApprovals");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.ApprovedByUsername)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(p => p.ConsumedBySalesOrderNumber)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(p => p.IssuedAt).IsRequired();
        builder.Property(p => p.ExpiresAt).IsRequired();

        // Looked up by Id (the token) on every checkout attempt, and by ExpiresAt when pruning
        // stale rows — both hot paths for a table that's small but write-heavy.
        builder.HasIndex(p => p.ExpiresAt);
    }
}
