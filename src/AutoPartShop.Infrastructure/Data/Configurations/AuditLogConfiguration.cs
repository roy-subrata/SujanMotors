using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EntityName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.EntityId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Action)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.PropertyName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.OldValue)
            .HasMaxLength(2000);

        builder.Property(x => x.NewValue)
            .HasMaxLength(2000);

        builder.Property(x => x.PerformedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.PerformedAt)
            .IsRequired();

        builder.Property(x => x.IpAddress)
            .HasMaxLength(50);

        builder.Property(x => x.UserAgent)
            .HasMaxLength(500);

        // Indexes for better query performance
        builder.HasIndex(x => x.EntityName);
        builder.HasIndex(x => x.EntityId);
        builder.HasIndex(x => x.Action);
        builder.HasIndex(x => x.PerformedBy);
        builder.HasIndex(x => x.PerformedAt);
    }
}
