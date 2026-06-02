using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.SKU)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(x => x.Barcode)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(x => x.WeightKg)
            .HasColumnType("decimal(10,4)")
            .IsRequired(false);

        builder.Property(x => x.WidthCm)
            .HasColumnType("decimal(10,2)")
            .IsRequired(false);

        builder.Property(x => x.HeightCm)
            .HasColumnType("decimal(10,2)")
            .IsRequired(false);

        builder.Property(x => x.DepthCm)
            .HasColumnType("decimal(10,2)")
            .IsRequired(false);

        builder.Property(x => x.PricingMode)
            .HasMaxLength(10)
            .IsRequired()
            .HasDefaultValue("OVERRIDE");

        builder.Property(x => x.CostPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.SellingPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.HasWarrantyOverride)
            .IsRequired(false);

        builder.Property(x => x.WarrantyPeriodMonthsOverride)
            .IsRequired(false);

        builder.Property(x => x.WarrantyTypeOverride)
            .HasMaxLength(20)
            .IsRequired(false);

        builder.HasOne(x => x.Part)
            .WithMany(p => p.Variants)
            .HasForeignKey(x => x.PartId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => new { x.PartId, x.Code }).IsUnique();

        // SKU / Barcode unique among live variants (filtered: ignore NULLs and soft-deleted rows).
        // The app layer additionally enforces uniqueness against base products (Parts table),
        // which a single-table index cannot cover.
        builder.HasIndex(x => x.SKU)
            .IsUnique()
            .HasFilter("[SKU] IS NOT NULL AND [Isdeleted] = 0");

        builder.HasIndex(x => x.Barcode)
            .IsUnique()
            .HasFilter("[Barcode] IS NOT NULL AND [Isdeleted] = 0");
    }
}
