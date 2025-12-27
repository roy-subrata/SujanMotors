

using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace AutoPartShop.Infrastructure.Data.Configurations;
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id);

        // Configure properties
        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Description)
                    .HasMaxLength(500)
                    .IsRequired(false);

        builder.Property(c => c.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasOne(c => c.ParentCategory)
               .WithMany(c => c.SubCategories)
               .HasForeignKey(c => c.ParentCategoryId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_Categories_ParentCategory");

        builder.Property(c => c.BreadcrumbPath)
            .HasMaxLength(1000);

        builder.Property(c => c.DisplayOrder)
            .IsRequired();

        builder.Property(c => c.ChildCount)
            .IsRequired();

        builder.Property(c => c.IsActive)
            .IsRequired();

        builder.Property(c => c.DepthLevel)
            .IsRequired();

        builder.HasIndex(c => c.IsActive);
        builder.HasIndex(c => c.Name);

        builder.HasIndex(c => c.Code).IsUnique();

        // Configure relationships if any
        // Example: builder.HasMany(c => c.Parts).WithOne(p => p.Category);
    }
}