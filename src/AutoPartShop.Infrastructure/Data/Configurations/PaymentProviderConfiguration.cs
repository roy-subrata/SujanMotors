using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartsShop.Infrastructure.Data.Configurations;

internal class PaymentProviderConfiguration : IEntityTypeConfiguration<PaymentProvider>
{
    public void Configure(EntityTypeBuilder<PaymentProvider> builder)
    {
        builder.ToTable("PaymentProviders");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.ProviderName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.ProviderType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Status)
            .HasMaxLength(20);

        builder.Property(p => p.ApiKey)
            .HasMaxLength(500);

        builder.Property(p => p.MerchantId)
            .HasMaxLength(100);

        builder.Property(p => p.BankName)
            .HasMaxLength(100);

        builder.Property(p => p.BankAccountNumber)
            .HasMaxLength(50);

        builder.Property(p => p.BankRoutingNumber)
            .HasMaxLength(50);

        builder.Property(p => p.BankIBAN)
            .HasMaxLength(50);

        builder.Property(p => p.BankSWIFT)
            .HasMaxLength(20);

        builder.Property(p => p.BeneficiaryName)
            .HasMaxLength(100);

        // Mobile Banking fields
        builder.Property(p => p.MobileNumber)
            .HasMaxLength(20);

        builder.Property(p => p.AccountHolderName)
            .HasMaxLength(100);

        builder.Property(p => p.AgentNumber)
            .HasMaxLength(50);

        builder.Property(p => p.TransactionFeeType)
            .HasMaxLength(20);

        builder.Property(p => p.TransactionFeeAmount)
            .HasColumnType("decimal(18,4)");

        builder.Property(p => p.MinimumAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.MaximumAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.SupportedCurrencies)
            .HasMaxLength(200);

        builder.Property(p => p.WebhookUrl)
            .HasMaxLength(500);

        builder.Property(p => p.Notes)
            .HasMaxLength(500);
    }
}
