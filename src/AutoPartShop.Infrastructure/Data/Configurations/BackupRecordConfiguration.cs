namespace AutoPartShop.Infrastructure.Data.Configurations;

using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Entity configuration for BackupRecord
/// </summary>
public class BackupRecordConfiguration : IEntityTypeConfiguration<BackupRecord>
{
    public void Configure(EntityTypeBuilder<BackupRecord> builder)
    {
        builder.ToTable("BackupRecords");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.FileName)
            .IsRequired()
            .HasMaxLength(260);

        builder.Property(b => b.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(b => b.TriggerType)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(b => b.GoogleDriveFileId)
            .HasMaxLength(100);

        builder.Property(b => b.ErrorMessage)
            .HasMaxLength(2000);

        builder.HasIndex(b => b.StartedAt);

        builder.HasIndex(b => b.Status);
    }
}
