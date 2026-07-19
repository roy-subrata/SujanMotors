using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class WarehouseLocationConfiguration : IEntityTypeConfiguration<WarehouseLocation>
{
    public void Configure(EntityTypeBuilder<WarehouseLocation> builder)
    {
        builder.ToTable("WarehouseLocations")
            .HasKey(x => x.Id);

        builder.Property(x => x.Zone)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(x => x.Aisle)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(x => x.Rack)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(x => x.Bin)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(x => x.Notes)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasIndex(x => x.WarehouseId);
        builder.HasIndex(x => x.CategoryId);
    }
}
