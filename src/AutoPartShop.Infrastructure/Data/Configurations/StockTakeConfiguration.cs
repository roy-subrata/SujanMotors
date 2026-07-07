using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class StockTakeConfiguration : IEntityTypeConfiguration<StockTake>
{
    public void Configure(EntityTypeBuilder<StockTake> builder)
    {
        builder.ToTable("StockTakes");

        builder.HasKey(st => st.Id);
        builder.Property(st => st.RowVersion).IsRowVersion();

        builder.Property(st => st.StockTakeNumber)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(st => st.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(st => st.CompletedBy)
            .HasMaxLength(100);

        builder.Property(st => st.Notes)
            .HasMaxLength(1000);

        builder.HasOne(st => st.Warehouse)
            .WithMany()
            .HasForeignKey(st => st.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(st => st.Category)
            .WithMany()
            .HasForeignKey(st => st.CategoryId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasMany(st => st.Lines)
            .WithOne(l => l.StockTake)
            .HasForeignKey(l => l.StockTakeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(st => st.StockTakeNumber).IsUnique();
        builder.HasIndex(st => st.WarehouseId);
        builder.HasIndex(st => st.Status);
    }
}

public class StockTakeLineConfiguration : IEntityTypeConfiguration<StockTakeLine>
{
    public void Configure(EntityTypeBuilder<StockTakeLine> builder)
    {
        builder.ToTable("StockTakeLines");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.PartName)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(l => l.PartCode)
            .HasMaxLength(100);

        builder.Property(l => l.VariantName)
            .HasMaxLength(200);

        builder.Property(l => l.Location)
            .HasMaxLength(100);

        builder.Property(l => l.CountedBy)
            .HasMaxLength(100);

        builder.Property(l => l.Notes)
            .HasMaxLength(500);

        builder.Property(l => l.UnitCost)
            .HasPrecision(18, 2);

        builder.Ignore(l => l.Variance);

        builder.HasOne(l => l.Part)
            .WithMany()
            .HasForeignKey(l => l.PartId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Variant)
            .WithMany()
            .HasForeignKey(l => l.VariantId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasIndex(l => l.StockTakeId);
        builder.HasIndex(l => new { l.StockTakeId, l.StockLevelId }).IsUnique();
    }
}
