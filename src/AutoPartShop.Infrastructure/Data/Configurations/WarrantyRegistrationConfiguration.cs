using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class WarrantyRegistrationConfiguration : IEntityTypeConfiguration<WarrantyRegistration>
{
    public void Configure(EntityTypeBuilder<WarrantyRegistration> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.WarrantyNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(w => w.WarrantyNumber)
            .IsUnique();

        builder.Property(w => w.PartId)
            .IsRequired();

        builder.Property(w => w.SalesOrderId)
            .IsRequired();

        builder.Property(w => w.SalesOrderLineId)
            .IsRequired();

        builder.Property(w => w.CustomerId)
            .IsRequired();

        builder.Property(w => w.SaleDate)
            .IsRequired();

        builder.Property(w => w.WarrantyStartDate)
            .IsRequired();

        builder.Property(w => w.WarrantyExpiryDate)
            .IsRequired();

        builder.Property(w => w.WarrantyType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(w => w.WarrantyPeriodMonths)
            .IsRequired();

        builder.Property(w => w.WarrantyTerms)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(w => w.CertificateNumber)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(w => w.Status)
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue("ACTIVE");

        builder.Property(w => w.VoidReason)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(w => w.VoidedDate)
            .IsRequired(false);

        // Foreign key relationships
        builder.HasOne(w => w.Part)
            .WithMany()
            .HasForeignKey(w => w.PartId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(w => w.SalesOrder)
            .WithMany()
            .HasForeignKey(w => w.SalesOrderId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(w => w.SalesOrderLine)
            .WithMany()
            .HasForeignKey(w => w.SalesOrderLineId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(w => w.Customer)
            .WithMany()
            .HasForeignKey(w => w.CustomerId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(w => w.Claims)
            .WithOne(c => c.WarrantyRegistration)
            .HasForeignKey(c => c.WarrantyRegistrationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for better query performance
        builder.HasIndex(w => w.CustomerId);
        builder.HasIndex(w => w.PartId);
        builder.HasIndex(w => w.SalesOrderId);
        builder.HasIndex(w => w.Status);
        builder.HasIndex(w => w.WarrantyExpiryDate);
    }
}
