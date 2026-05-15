using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class DiscountConfiguration : IEntityTypeConfiguration<Discount>
{
    public void Configure(EntityTypeBuilder<Discount> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name).IsRequired().HasMaxLength(200);
        builder.Property(d => d.Description).HasMaxLength(500);
        builder.Property(d => d.Type).IsRequired().HasMaxLength(20);
        builder.Property(d => d.PromoCode).HasMaxLength(50);

        builder.Property(d => d.Value).HasPrecision(18, 2);
        builder.Property(d => d.MinimumCartAmount).HasPrecision(18, 2);

        // Optional FK to Part
        builder.HasOne(d => d.Part)
            .WithMany()
            .HasForeignKey(d => d.PartId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Optional FK to ProductVariant
        builder.HasOne(d => d.ProductVariant)
            .WithMany()
            .HasForeignKey(d => d.ProductVariantId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.PromoCode)
            .IsUnique()
            .HasFilter("[PromoCode] IS NOT NULL AND [Isdeleted] = 0");

        // Fast lookup by part + variant scope
        builder.HasIndex(d => new { d.PartId, d.ProductVariantId });
        builder.HasIndex(d => d.StartDate);
    }
}
