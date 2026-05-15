using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class ProductVariantPriceHistoryConfiguration : IEntityTypeConfiguration<ProductVariantPriceHistory>
{
    public void Configure(EntityTypeBuilder<ProductVariantPriceHistory> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SellingPrice).HasPrecision(18, 2);
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(10);
        builder.Property(x => x.Reason).HasMaxLength(500);

        // Required FK to Part
        builder.HasOne(x => x.Part)
            .WithMany()
            .HasForeignKey(x => x.PartId)
            .OnDelete(DeleteBehavior.Restrict);

        // Optional FK to ProductVariant (null = base product price)
        builder.HasOne(x => x.ProductVariant)
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Fast lookup: active price for part + variant scope
        builder.HasIndex(x => new { x.PartId, x.ProductVariantId, x.EndDate });

        // Fast lookup: price on a specific date
        builder.HasIndex(x => new { x.PartId, x.ProductVariantId, x.StartDate, x.EndDate });
    }
}
