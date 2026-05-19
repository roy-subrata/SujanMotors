using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class VariantAttributeValueConfiguration : IEntityTypeConfiguration<VariantAttributeValue>
{
    public void Configure(EntityTypeBuilder<VariantAttributeValue> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ValueText)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.ValueNumber)
            .HasColumnType("decimal(18,4)")
            .IsRequired(false);

        builder.HasOne(x => x.Variant)
            .WithMany(v => v.Attributes)
            .HasForeignKey(x => x.VariantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Attribute)
            .WithMany()
            .HasForeignKey(x => x.AttributeId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Option)
            .WithMany()
            .HasForeignKey(x => x.OptionId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => new { x.VariantId, x.AttributeId }).IsUnique();
    }
}
