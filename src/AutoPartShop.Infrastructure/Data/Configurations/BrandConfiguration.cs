using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartsShop.Infrastructure.Data.Configurations;

public class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("Brands");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(b => b.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(b => b.Description)
            .HasMaxLength(500);

        builder.Property(b => b.LogoUrl)
            .HasMaxLength(500);

        builder.Property(b => b.Website)
            .HasMaxLength(200);

        builder.Property(b => b.Country)
            .HasMaxLength(100);

        builder.Property(b => b.ContactEmail)
            .HasMaxLength(200);

        builder.Property(b => b.ContactPhone)
            .HasMaxLength(50);

        builder.Property(b => b.DisplayOrder)
            .HasDefaultValue(0);

        builder.Property(b => b.IsActive)
            .HasDefaultValue(true);

        // Unique index on Code
        builder.HasIndex(b => b.Code)
            .IsUnique()
            .HasFilter("[Isdeleted] = 0");

        // Index on Name for searching
        builder.HasIndex(b => b.Name);

        // Relationship with Parts
        builder.HasMany(b => b.Parts)
            .WithOne(p => p.Brand)
            .HasForeignKey(p => p.BrandId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
