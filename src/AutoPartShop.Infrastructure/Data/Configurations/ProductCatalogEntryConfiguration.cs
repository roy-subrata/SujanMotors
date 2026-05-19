using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class ProductCatalogEntryConfiguration : IEntityTypeConfiguration<ProductCatalogEntry>
{
    public void Configure(EntityTypeBuilder<ProductCatalogEntry> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Slug)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ShortDescription)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.PrimaryImageUrl)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.VideoUrl)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.MetaTitle)
            .HasMaxLength(160)
            .IsRequired(false);

        builder.Property(x => x.MetaDescription)
            .HasMaxLength(320)
            .IsRequired(false);

        builder.Property(x => x.PopularityScore)
            .HasColumnType("decimal(18,4)")
            .IsRequired();

        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => new { x.IsPublished, x.IsFeatured, x.FeaturedRank });

        builder.HasOne(x => x.Part)
            .WithOne(p => p.CatalogEntry)
            .HasForeignKey<ProductCatalogEntry>(x => x.PartId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
