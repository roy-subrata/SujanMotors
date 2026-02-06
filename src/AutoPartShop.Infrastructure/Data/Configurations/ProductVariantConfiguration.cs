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

        builder.Property(x => x.CostPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired(false);

        builder.Property(x => x.SellingPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired(false);

        builder.Property(x => x.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.HasOne(x => x.Part)
            .WithMany(p => p.Variants)
            .HasForeignKey(x => x.PartId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => new { x.PartId, x.Code }).IsUnique();
        builder.HasIndex(x => x.SKU).IsUnique(false);
    }
}
