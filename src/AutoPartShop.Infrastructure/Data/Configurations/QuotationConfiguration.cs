using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class QuotationConfiguration : IEntityTypeConfiguration<Quotation>
{
    public void Configure(EntityTypeBuilder<Quotation> builder)
    {
        builder.ToTable("Quotations");
        builder.HasKey(q => q.Id);

        builder.Property(q => q.QuotationNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(q => q.CustomerName).IsRequired().HasMaxLength(200);
        builder.Property(q => q.CustomerEmail).HasMaxLength(200);
        builder.Property(q => q.CustomerPhone).HasMaxLength(50);

        builder.Property(q => q.Status).IsRequired().HasMaxLength(20);

        builder.Property(q => q.SubTotal).HasPrecision(18, 2);
        builder.Property(q => q.DiscountPercentage).HasPrecision(18, 2);
        builder.Property(q => q.DiscountAmount).HasPrecision(18, 2);
        builder.Property(q => q.TotalAmount).HasPrecision(18, 2);
        builder.Property(q => q.TaxAmount).HasPrecision(18, 2);

        builder.Property(q => q.Currency).IsRequired().HasMaxLength(3).HasDefaultValue("BDT");
        builder.Property(q => q.Notes).HasMaxLength(1000);

        builder.HasOne(q => q.Customer)
            .WithMany()
            .HasForeignKey(q => q.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // A quotation converts into at most one SalesOrder; the SO is never deleted for keeping a
        // quote's history, so Restrict rather than cascading into the order.
        builder.HasOne(q => q.ConvertedToSalesOrder)
            .WithMany()
            .HasForeignKey(q => q.ConvertedToSalesOrderId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasMany(q => q.LineItems)
            .WithOne(l => l.Quotation)
            .HasForeignKey(l => l.QuotationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(q => q.QuotationNumber).IsUnique();
        builder.HasIndex(q => q.CustomerId);
        builder.HasIndex(q => q.Status);
        builder.HasIndex(q => q.ValidUntil);
    }
}

public class QuotationLineConfiguration : IEntityTypeConfiguration<QuotationLine>
{
    public void Configure(EntityTypeBuilder<QuotationLine> builder)
    {
        builder.ToTable("QuotationLines");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.UnitPrice).HasPrecision(18, 2);
        builder.Property(l => l.Discount).HasPrecision(18, 2);
        builder.Property(l => l.Description).HasMaxLength(500);

        builder.HasOne(l => l.Part)
            .WithMany()
            .HasForeignKey(l => l.PartId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.ProductVariant)
            .WithMany()
            .HasForeignKey(l => l.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(l => l.Unit)
            .WithMany()
            .HasForeignKey(l => l.UnitId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasIndex(l => l.QuotationId);
        builder.HasIndex(l => l.PartId);
    }
}
