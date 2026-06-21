using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class StockLevelConfiguration : IEntityTypeConfiguration<StockLevel>
{
    public void Configure(EntityTypeBuilder<StockLevel> builder)
    {
        builder.ToTable("StockLevels");

        builder.HasKey(sl => sl.Id);
        builder.Property(sl => sl.RowVersion).IsRowVersion();

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

        builder.HasOne(sl => sl.Unit)
            .WithMany()
            .HasForeignKey(sl => sl.UnitId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasMany(sl => sl.Movements)
            .WithOne(m => m.StockLevel)
            .HasForeignKey(m => m.StockLevelId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes — stock-keeping unit is (Part, Variant?, Warehouse). Two filtered unique indexes:
        //  • variant rows: unique per (Part, Variant, Warehouse)
        //  • part-level rows (VariantId NULL): unique per (Part, Warehouse)
        // Split is needed because a single unique index over a nullable column lets multiple NULLs through.
        builder.HasIndex(sl => new { sl.PartId, sl.VariantId, sl.WarehouseId })
            .IsUnique()
            .HasFilter("[VariantId] IS NOT NULL");
        builder.HasIndex(sl => new { sl.PartId, sl.WarehouseId })
            .IsUnique()
            .HasFilter("[VariantId] IS NULL");
        builder.HasIndex(sl => sl.PartId);
        builder.HasIndex(sl => sl.VariantId);
        builder.HasIndex(sl => sl.WarehouseId);
    }
}

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements");

        builder.HasKey(sm => sm.Id);

        builder.Property(sm => sm.MovementType)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(sm => sm.Reason)
            .HasMaxLength(100);

        builder.Property(sm => sm.ReferenceNumber)
            .HasMaxLength(100);

        builder.Property(sm => sm.ApprovedBy)
            .HasMaxLength(100);

        builder.Property(sm => sm.Notes)
            .HasMaxLength(500);

        // Relationships
        builder.HasOne(sm => sm.StockLevel)
            .WithMany(sl => sl.Movements)
            .HasForeignKey(sm => sm.StockLevelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sm => sm.Unit)
            .WithMany()
            .HasForeignKey(sm => sm.UnitId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // Indexes
        builder.HasIndex(sm => sm.StockLevelId);
        builder.HasIndex(sm => sm.MovementDate);
        builder.HasIndex(sm => sm.MovementType);
        builder.HasIndex(sm => sm.PurchaseOrderLineId);
        builder.HasIndex(sm => sm.SalesOrderLineId);
    }
}
