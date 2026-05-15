using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class ProductMediaConfiguration : IEntityTypeConfiguration<ProductMedia>
{
    public void Configure(EntityTypeBuilder<ProductMedia> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Url)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.MediaType)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.AltText)
            .HasMaxLength(300)
            .IsRequired(false);

        builder.Property(x => x.FileName)
            .HasMaxLength(255)
            .IsRequired(false);

        builder.HasOne(x => x.Part)
            .WithMany(p => p.Media)
            .HasForeignKey(x => x.PartId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Variant)
            .WithMany(v => v.Media)
            .HasForeignKey(x => x.VariantId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => new { x.PartId, x.SortOrder });
    }
}
