using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class PayrollRunConfiguration : IEntityTypeConfiguration<PayrollRun>
{
    public void Configure(EntityTypeBuilder<PayrollRun> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.RunCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(p => p.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(p => p.TotalGross).HasPrecision(18, 2);
        builder.Property(p => p.TotalDeductions).HasPrecision(18, 2);
        builder.Property(p => p.TotalNet).HasPrecision(18, 2);

        builder.Property(p => p.ApprovedBy).HasMaxLength(100);
        builder.Property(p => p.PaidBy).HasMaxLength(100);
        builder.Property(p => p.PaymentMethod).HasMaxLength(30);
        builder.Property(p => p.Notes).HasMaxLength(1000);

        builder.HasMany(p => p.Payslips)
            .WithOne()
            .HasForeignKey(s => s.PayrollRunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.RunCode).IsUnique();

        // One run per month
        builder.HasIndex(p => new { p.Year, p.Month })
            .IsUnique()
            .HasFilter("[Isdeleted] = 0");

        builder.HasIndex(p => p.Status);
    }
}
