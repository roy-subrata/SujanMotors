using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class ProductAttributeConfiguration : IEntityTypeConfiguration<ProductAttribute>
{
    public void Configure(EntityTypeBuilder<ProductAttribute> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.DataType)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Unit)
            .HasMaxLength(30)
            .IsRequired(false);

        builder.HasOne(x => x.AttributeGroup)
            .WithMany(g => g.Attributes)
            .HasForeignKey(x => x.AttributeGroupId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => x.Code).IsUnique();
    }
}
