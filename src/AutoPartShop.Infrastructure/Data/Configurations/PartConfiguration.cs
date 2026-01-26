


using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace AutoPartShop.Infrastructure.Data.Configurations;
public class PartEntityConfiguration : IEntityTypeConfiguration<Part>
{
    public void Configure(EntityTypeBuilder<Part> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
              .HasMaxLength(150)
              .IsRequired();

        builder.OwnsOne(p => p.PartNumber, owned =>
        {
            owned.Property(x => x.Value)
                 .HasColumnName("PartNumber")
                 .HasMaxLength(30)
                 .IsRequired();
        });

        builder.Property(p => p.Description)
              .HasMaxLength(255)
              .IsRequired(false);

        builder.Property(p => p.SKU)
        .HasMaxLength(100)
        .IsRequired();

        builder.Property(p => p.CostPrice)
               .HasColumnType("decimal(18,2)")  // SQL column type
               .IsRequired();

        builder.Property(p => p.CostPriceCurrency)
               .HasMaxLength(3)
               .IsRequired()
               .HasDefaultValue("BDT");

        builder.Property(p => p.SellingPrice)
                     .HasColumnType("decimal(18,2)")  // SQL column type
                     .IsRequired();

        builder.Property(p => p.SellingPriceCurrency)
               .HasMaxLength(3)
               .IsRequired()
               .HasDefaultValue("BDT");

        // Warranty Configuration
        builder.Property(p => p.HasWarranty)
               .IsRequired()
               .HasDefaultValue(false);

        builder.Property(p => p.WarrantyPeriodMonths)
               .IsRequired(false);

        builder.Property(p => p.WarrantyType)
               .HasMaxLength(50)
               .IsRequired(false);

        builder.Property(p => p.WarrantyTerms)
               .HasMaxLength(2000)
               .IsRequired(false);

        builder.Property(p => p.WarrantyCertificateTemplate)
               .HasMaxLength(100)
               .IsRequired(false);

        builder.HasMany(b => b.VehicleCompatibilities)
        .WithOne(p => p.Part)
        .HasForeignKey(p => p.PartId)
        .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(p => p.Unit)
            .WithMany(p => p.Parts)
            .HasForeignKey(p => p.UnitId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Property(p => p.UnitId);

        builder.HasOne(p => p.Category)
                .WithMany()
                .HasForeignKey(p => p.CategoryId)
        .OnDelete(DeleteBehavior.NoAction);

        builder.Property(p => p.CategoryId);

        builder.HasIndex(p => p.SKU).IsUnique();

    }
}