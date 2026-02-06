using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class ProductAttributeOptionConfiguration : IEntityTypeConfiguration<ProductAttributeOption>
{
    public void Configure(EntityTypeBuilder<ProductAttributeOption> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Value)
            .HasMaxLength(150)
            .IsRequired();

        builder.HasOne(x => x.Attribute)
            .WithMany(a => a.Options)
            .HasForeignKey(x => x.AttributeId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => new { x.AttributeId, x.Value }).IsUnique();
    }
}
