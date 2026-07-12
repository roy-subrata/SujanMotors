using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Date)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(a => a.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(a => a.Notes)
            .HasMaxLength(500);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        // One record per employee per day (soft-deleted rows excluded)
        builder.HasIndex(a => new { a.EmployeeId, a.Date })
            .IsUnique()
            .HasFilter("[Isdeleted] = 0");

        builder.HasIndex(a => a.Date);
        builder.HasIndex(a => a.Status);
    }
}
