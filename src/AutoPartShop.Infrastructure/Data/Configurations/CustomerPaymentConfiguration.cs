using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class CustomerPaymentConfiguration : IEntityTypeConfiguration<CustomerPayment>
{
    public void Configure(EntityTypeBuilder<CustomerPayment> builder)
    {
        builder.HasKey(cp => cp.Id);

        builder.Property(cp => cp.TransactionNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(cp => cp.Amount)
            .HasPrecision(18, 2);

        builder.Property(cp => cp.PaymentFee)
            .HasPrecision(18, 2);

        builder.Property(cp => cp.NetAmount)
            .HasPrecision(18, 2);

        builder.Property(cp => cp.Currency)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(cp => cp.PaymentMethod)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(cp => cp.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(cp => cp.ReferenceNumber)
            .HasMaxLength(100);

        builder.Property(cp => cp.AuthorizationCode)
            .HasMaxLength(100);

        // Relationships
        builder.HasOne(cp => cp.Customer)
            .WithMany(c => c.CustomerPayments)
            .HasForeignKey(cp => cp.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cp => cp.Invoice)
            .WithMany(i => i.CustomerPayments)
            .HasForeignKey(cp => cp.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(cp => cp.PaymentProvider)
            .WithMany()
            .HasForeignKey(cp => cp.PaymentProviderId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // Indexes
        builder.HasIndex(cp => cp.TransactionNumber).IsUnique();
        builder.HasIndex(cp => cp.CustomerId);
        builder.HasIndex(cp => cp.Status);
        builder.HasIndex(cp => cp.PaymentDate);
    }
}
