


using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace AutoPartShop.Infrastructure.Data.Configurations;

public class PartEntityConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
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

        builder.Property(p => p.RichDescription)
              .HasColumnType("nvarchar(max)")
              .IsRequired(false);

        builder.Property(p => p.SKU)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.OemNumber)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(p => p.LocalName)
            .HasMaxLength(200)
            .IsRequired(false);

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

        // Universal product fields
        builder.Property(p => p.Barcode)
               .HasMaxLength(100)
               .IsRequired(false);

        builder.Property(p => p.Tags)
               .HasMaxLength(500)
               .IsRequired(false);

        builder.Property(p => p.ProductType)
               .HasMaxLength(20)
               .IsRequired()
               .HasDefaultValue("PHYSICAL");

        builder.Property(p => p.TaxCode)
               .HasMaxLength(50)
               .IsRequired(false);

        builder.Property(p => p.WeightKg)
               .HasColumnType("decimal(10,4)")
               .IsRequired(false);

        builder.Property(p => p.WidthCm)
               .HasColumnType("decimal(10,2)")
               .IsRequired(false);

        builder.Property(p => p.HeightCm)
               .HasColumnType("decimal(10,2)")
               .IsRequired(false);

        builder.Property(p => p.DepthCm)
               .HasColumnType("decimal(10,2)")
               .IsRequired(false);

        builder.Property(p => p.IsPerishable)
               .IsRequired()
               .HasDefaultValue(false);

        builder.HasMany(b => b.VehicleCompatibilities)
        .WithOne(p => p.Part)
        .HasForeignKey(p => p.PartId)
        .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(b => b.BaseUnit)
            .WithMany()
            .HasForeignKey(p => p.BaseUnitId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false)
            .HasConstraintName("FK_Part_BaseUnit");

        builder.HasOne(p => p.Unit)
            .WithMany()
            .HasForeignKey(p => p.UnitId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Part_Unit");

        builder.HasOne(p => p.Category)
                .WithMany()
                .HasForeignKey(p => p.CategoryId)
        .OnDelete(DeleteBehavior.NoAction);

        builder.Property(p => p.CategoryId);

        builder.HasIndex(p => p.SKU).IsUnique();

    }
}
