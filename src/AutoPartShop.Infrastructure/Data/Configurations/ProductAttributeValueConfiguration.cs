using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class ProductAttributeValueConfiguration : IEntityTypeConfiguration<ProductAttributeValue>
{
    public void Configure(EntityTypeBuilder<ProductAttributeValue> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ValueText)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.ValueNumber)
            .HasColumnType("decimal(18,4)")
            .IsRequired(false);

        builder.HasOne(x => x.Product)
            .WithMany(p => p.AttributeValues)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Attribute)
            .WithMany()
            .HasForeignKey(x => x.AttributeId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Option)
            .WithMany()
            .HasForeignKey(x => x.OptionId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => new { x.ProductId, x.AttributeId }).IsUnique();
    }
}
