using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.ShipmentNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("PENDING");

        builder.Property(s => s.CourierName)
            .HasMaxLength(200);

        builder.Property(s => s.TrackingNumber)
            .HasMaxLength(200);

        builder.Property(s => s.FailureReason)
            .HasMaxLength(500);

        builder.Property(s => s.Notes)
            .HasMaxLength(1000);

        builder.HasOne(s => s.SalesOrder)
            .WithMany()
            .HasForeignKey(s => s.SalesOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Lines)
            .WithOne(l => l.Shipment)
            .HasForeignKey(l => l.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.ShipmentNumber).IsUnique();
        builder.HasIndex(s => s.SalesOrderId);
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.TrackingNumber);
    }
}

public class ShipmentLineConfiguration : IEntityTypeConfiguration<ShipmentLine>
{
    public void Configure(EntityTypeBuilder<ShipmentLine> builder)
    {
        builder.ToTable("ShipmentLines");
        builder.HasKey(l => l.Id);

        builder.HasOne(l => l.SalesOrderLine)
            .WithMany()
            .HasForeignKey(l => l.SalesOrderLineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Part)
            .WithMany()
            .HasForeignKey(l => l.PartId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.ProductVariant)
            .WithMany()
            .HasForeignKey(l => l.ProductVariantId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);

        builder.HasIndex(l => l.SalesOrderLineId);
        builder.HasIndex(l => l.PartId);
    }
}
