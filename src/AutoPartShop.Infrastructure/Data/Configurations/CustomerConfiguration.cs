using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.CustomerCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(c => c.Phone)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(c => c.AlternatePhone)
            .HasMaxLength(20);

        builder.Property(c => c.CompanyName)
            .HasMaxLength(200);

        builder.Property(c => c.CurrentBalance)
            .HasPrecision(18, 2);

        builder.Property(c => c.TotalPurchaseAmount)
            .HasPrecision(18, 2);

        builder.Property(c => c.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(c => c.CustomerType)
            .IsRequired()
            .HasMaxLength(20);

        // Ignore computed properties
        builder.Ignore(c => c.TotalPaid);
        builder.Ignore(c => c.AccountBalance);
        builder.Ignore(c => c.AdvanceAmount);
        builder.Ignore(c => c.PendingPaymentsCount);

        // Note: Relationships are configured from the dependent side
        // (SalesOrderConfiguration and CustomerPaymentConfiguration)
        // to avoid duplicate configurations causing shadow FK properties

        // Indexes
        builder.HasIndex(c => c.CustomerCode).IsUnique();
        builder.HasIndex(c => c.Email);
        builder.HasIndex(c => c.Status);
    }
}
