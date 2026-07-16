using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartsShop.Infrastructure.Data.Configurations
{
    internal class CashDepositConfiguration : IEntityTypeConfiguration<CashDeposit>
    {
        public void Configure(EntityTypeBuilder<CashDeposit> builder)
        {
            builder.ToTable("CashDeposits");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Category)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(x => x.Description)
                   .HasMaxLength(500)
                   .IsRequired();

            builder.Property(x => x.PaymentMethod)
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(x => x.ReferenceNumber)
                   .HasMaxLength(200);

            builder.Property(x => x.Notes)
                   .HasMaxLength(1000);

            builder.Property(x => x.Amount)
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(x => x.Currency)
                   .HasMaxLength(3)
                   .IsRequired()
                   .HasDefaultValue("BDT");

            builder.HasIndex(x => x.DepositDate);
            builder.HasIndex(x => x.Category);
        }
    }
}
