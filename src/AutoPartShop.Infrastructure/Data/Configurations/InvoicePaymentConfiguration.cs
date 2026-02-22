using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class InvoicePaymentConfiguration : IEntityTypeConfiguration<InvoicePayment>
{
    public void Configure(EntityTypeBuilder<InvoicePayment> builder)
    {
        builder.HasKey(ip => ip.Id);

        builder.Property(ip => ip.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(ip => ip.PaymentMethod)
            .HasMaxLength(50);

        builder.Property(ip => ip.ReferenceNumber)
            .HasMaxLength(100);

        builder.Property(ip => ip.Notes)
            .HasMaxLength(500);

        builder.Property(ip => ip.PaymentDate)
            .IsRequired();

        // Index
        builder.HasIndex(ip => ip.InvoiceId);
        builder.HasIndex(ip => ip.PaymentDate);
    }
}
