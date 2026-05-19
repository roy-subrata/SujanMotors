using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class VariantStockLevelConfiguration : IEntityTypeConfiguration<VariantStockLevel>
{
    public void Configure(EntityTypeBuilder<VariantStockLevel> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Variant)
            .WithMany(v => v.StockLevels)
            .HasForeignKey(x => x.VariantId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => new { x.VariantId, x.WarehouseId }).IsUnique();
    }
}
