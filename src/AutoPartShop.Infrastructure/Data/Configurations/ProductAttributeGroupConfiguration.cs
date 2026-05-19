using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class ProductAttributeGroupConfiguration : IEntityTypeConfiguration<ProductAttributeGroup>
{
    public void Configure(EntityTypeBuilder<ProductAttributeGroup> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.HasIndex(x => x.Name).IsUnique();
    }
}
