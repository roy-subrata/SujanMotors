using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class SalaryAdvanceConfiguration : IEntityTypeConfiguration<SalaryAdvance>
{
    public void Configure(EntityTypeBuilder<SalaryAdvance> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.AdvanceDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(a => a.Amount)
            .HasPrecision(18, 2);

        builder.Property(a => a.PaymentMethod)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(a => a.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(a => a.Notes)
            .HasMaxLength(500);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => a.EmployeeId);
        builder.HasIndex(a => a.Status);
    }
}
