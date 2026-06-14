using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class StockLotConfiguration : IEntityTypeConfiguration<StockLot>
{
    public void Configure(EntityTypeBuilder<StockLot> builder)
    {
        builder.HasKey(sl => sl.Id);

        builder.Property(sl => sl.LotNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(sl => sl.CostPrice)
            .HasPrecision(18, 2);

        builder.Property(sl => sl.CostPriceInBaseUnit)
            .HasPrecision(18, 2);

        builder.Property(sl => sl.Currency)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(sl => sl.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("AVAILABLE");

        // Relationships
        builder.HasOne(sl => sl.Part)
            .WithMany()
            .HasForeignKey(sl => sl.PartId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sl => sl.Variant)
            .WithMany()
            .HasForeignKey(sl => sl.VariantId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(sl => sl.Warehouse)
            .WithMany()
            .HasForeignKey(sl => sl.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sl => sl.Supplier)
            .WithMany()
            .HasForeignKey(sl => sl.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sl => sl.Unit)
            .WithMany()
            .HasForeignKey(sl => sl.UnitId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasMany(sl => sl.Movements)
            .WithOne(m => m.StockLot)
            .HasForeignKey(m => m.StockLotId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(sl => sl.LotNumber).IsUnique();
        builder.HasIndex(sl => sl.PartId);
        builder.HasIndex(sl => new { sl.PartId, sl.VariantId, sl.WarehouseId });
        builder.HasIndex(sl => sl.VariantId);
        builder.HasIndex(sl => sl.WarehouseId);
        builder.HasIndex(sl => sl.SupplierId);
        builder.HasIndex(sl => sl.Status);
    }
}
