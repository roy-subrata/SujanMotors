using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class CategoryAttributeConfiguration : IEntityTypeConfiguration<CategoryAttribute>
{
    public void Configure(EntityTypeBuilder<CategoryAttribute> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FilterType)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Attribute)
            .WithMany()
            .HasForeignKey(x => x.AttributeId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => new { x.CategoryId, x.AttributeId }).IsUnique();
    }
}
