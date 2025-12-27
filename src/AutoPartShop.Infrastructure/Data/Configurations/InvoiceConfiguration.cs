using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.InvoiceNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(i => i.SubTotal)
            .HasPrecision(18, 2);

        builder.Property(i => i.TaxAmount)
            .HasPrecision(18, 2);

        builder.Property(i => i.DiscountAmount)
            .HasPrecision(18, 2);

        builder.Property(i => i.Status)
            .IsRequired()
            .HasMaxLength(20);

        // Ignore computed properties
        builder.Ignore(i => i.AmountPaid);
        builder.Ignore(i => i.TotalAmount);
        builder.Ignore(i => i.OutstandingAmount);
        builder.Ignore(i => i.CreditBalance);
        builder.Ignore(i => i.HasCredit);

        // Relationships
        builder.HasOne(i => i.SalesOrder)
            .WithOne(so => so.Invoice)
            .HasForeignKey<Invoice>(i => i.SalesOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(i => i.CustomerPayments)
            .WithOne(cp => cp.Invoice)
            .HasForeignKey(cp => cp.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(i => i.InvoiceNumber).IsUnique();
        builder.HasIndex(i => i.SalesOrderId);
        builder.HasIndex(i => i.Status);
        builder.HasIndex(i => i.InvoiceDate);
    }
}
