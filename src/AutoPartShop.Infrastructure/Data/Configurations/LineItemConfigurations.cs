using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class SalesOrderLineConfiguration : IEntityTypeConfiguration<SalesOrderLine>
{
    public void Configure(EntityTypeBuilder<SalesOrderLine> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(l => l.Discount)
            .HasPrecision(18, 2);

        builder.Property(l => l.Description)
            .HasMaxLength(500);

        // Part relationship
        builder.HasOne(l => l.Part)
            .WithMany()
            .HasForeignKey(l => l.PartId)
            .OnDelete(DeleteBehavior.Restrict);

        // Variant relationship — nullable, no cascade so variant changes don't affect order history
        builder.HasOne(l => l.ProductVariant)
            .WithMany()
            .HasForeignKey(l => l.ProductVariantId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);

        builder.HasIndex(l => l.ProductVariantId);

        // Unit relationship
        builder.HasOne(l => l.Unit)
            .WithMany()
            .HasForeignKey(l => l.UnitId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);
    }
}

public class SalesReturnLineConfiguration : IEntityTypeConfiguration<SalesReturnLine>
{
    public void Configure(EntityTypeBuilder<SalesReturnLine> builder)
    {
        builder.ToTable("SalesReturnLines");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(l => l.UnitPriceInBaseUnit)
            .HasPrecision(18, 2);

        builder.Property(l => l.Condition)
            .HasMaxLength(50);

        builder.Property(l => l.Notes)
            .HasMaxLength(500);

        // Part relationship
        builder.HasOne(l => l.Part)
            .WithMany()
            .HasForeignKey(l => l.PartId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unit relationship
        builder.HasOne(l => l.Unit)
            .WithMany()
            .HasForeignKey(l => l.UnitId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // Indexes
        builder.HasIndex(l => l.SalesOrderLineId);
        builder.HasIndex(l => l.PartId);
    }
}

public class PurchaseReturnLineConfiguration : IEntityTypeConfiguration<PurchaseReturnLine>
{
    public void Configure(EntityTypeBuilder<PurchaseReturnLine> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(l => l.Condition)
            .HasMaxLength(50);

        builder.Property(l => l.Notes)
            .HasMaxLength(500);

        // Part relationship
        builder.HasOne(l => l.Part)
            .WithMany()
            .HasForeignKey(l => l.PartId)
            .OnDelete(DeleteBehavior.Restrict);

        // Optional StockLot relationship - for specific lot selection
        builder.HasOne(l => l.StockLot)
            .WithMany()
            .HasForeignKey(l => l.StockLotId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}
