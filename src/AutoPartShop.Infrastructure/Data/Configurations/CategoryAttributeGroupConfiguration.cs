using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class CategoryAttributeGroupConfiguration : IEntityTypeConfiguration<CategoryAttributeGroup>
{
    public void Configure(EntityTypeBuilder<CategoryAttributeGroup> builder)
    {
        builder.ToTable("CategoryAttributeGroups")
            .HasKey(x => x.Id);

        builder.HasIndex(x => new { x.CategoryId, x.AttributeGroupId }).IsUnique();

        builder.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.AttributeGroup)
            .WithMany()
            .HasForeignKey(x => x.AttributeGroupId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
