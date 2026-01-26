using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class SalesOrderConfiguration : IEntityTypeConfiguration<SalesOrder>
{
    public void Configure(EntityTypeBuilder<SalesOrder> builder)
    {
        builder.HasKey(so => so.Id);

        builder.Property(so => so.SONumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(so => so.TotalAmount)
            .HasPrecision(18, 2);

        builder.Property(so => so.TaxAmount)
            .HasPrecision(18, 2);

        builder.Property(so => so.PaidAmount)
            .HasPrecision(18, 2);

        builder.Property(so => so.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(so => so.PaymentStatus)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(so => so.TechnicianName)
            .HasMaxLength(200);

        builder.Property(so => so.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("BDT");

        // Relationships
        builder.HasOne(so => so.Customer)
            .WithMany(c => c.SalesOrders)
            .HasForeignKey(so => so.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(so => so.Warehouse)
            .WithMany()
            .HasForeignKey(so => so.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(so => so.Technician)
            .WithMany(t => t.SalesOrders)
            .HasForeignKey(so => so.TechnicianId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasMany(so => so.LineItems)
            .WithOne(li => li.SalesOrder)
            .HasForeignKey(li => li.SalesOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(so => so.Invoice)
            .WithOne(i => i.SalesOrder)
            .HasForeignKey<Invoice>(i => i.SalesOrderId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // Indexes
        builder.HasIndex(so => so.SONumber).IsUnique();
        builder.HasIndex(so => so.CustomerId);
        builder.HasIndex(so => so.TechnicianId);
        builder.HasIndex(so => so.Status);
        builder.HasIndex(so => so.SODate);
    }
}
