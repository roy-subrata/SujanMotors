using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartsShop.Infrastructure.Data.Configurations;

internal class SupplierPaymentAccountConfiguration : IEntityTypeConfiguration<SupplierPaymentAccount>
{
    public void Configure(EntityTypeBuilder<SupplierPaymentAccount> builder)
    {
        builder.ToTable("SupplierPaymentAccounts");

        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Supplier)
            .WithMany(x => x.PaymentAccounts)
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.AccountType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.AccountName)
            .HasMaxLength(100);

        // Bank Transfer fields
        builder.Property(x => x.BankName)
            .HasMaxLength(100);

        builder.Property(x => x.BankAccountNumber)
            .HasMaxLength(50);

        builder.Property(x => x.BankBranchName)
            .HasMaxLength(100);

        builder.Property(x => x.BankBranchCode)
            .HasMaxLength(20);

        builder.Property(x => x.BeneficiaryName)
            .HasMaxLength(100);

        builder.Property(x => x.BankIBAN)
            .HasMaxLength(50);

        builder.Property(x => x.BankSWIFT)
            .HasMaxLength(20);

        // Mobile Banking fields
        builder.Property(x => x.MobileNumber)
            .HasMaxLength(20);

        builder.Property(x => x.MobileAccountHolderName)
            .HasMaxLength(100);

        builder.Property(x => x.MobileProvider)
            .HasMaxLength(50);

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(x => x.SupplierId);
        builder.HasIndex(x => new { x.SupplierId, x.IsDefault });
        builder.HasIndex(x => x.AccountType);
    }
}
