using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class PayslipConfiguration : IEntityTypeConfiguration<Payslip>
{
    public void Configure(EntityTypeBuilder<Payslip> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.EmployeeCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.EmployeeName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Designation).HasMaxLength(100);
        builder.Property(s => s.Department).HasMaxLength(100);

        builder.Property(s => s.MonthlySalary).HasPrecision(18, 2);
        builder.Property(s => s.OvertimeAmount).HasPrecision(18, 2);
        builder.Property(s => s.BonusAmount).HasPrecision(18, 2);
        builder.Property(s => s.OtherAllowance).HasPrecision(18, 2);
        builder.Property(s => s.AdvanceDeduction).HasPrecision(18, 2);
        builder.Property(s => s.OtherDeduction).HasPrecision(18, 2);
        builder.Property(s => s.AbsenceDeduction).HasPrecision(18, 2);
        builder.Property(s => s.GrossPay).HasPrecision(18, 2);
        builder.Property(s => s.TotalDeduction).HasPrecision(18, 2);
        builder.Property(s => s.NetPay).HasPrecision(18, 2);

        builder.Property(s => s.AdjustmentNotes).HasMaxLength(500);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(s => s.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.PayrollRunId);
        builder.HasIndex(s => s.EmployeeId);
    }
}
