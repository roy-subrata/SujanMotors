namespace AutoPartShop.Infrastructure.Data.Configurations;

using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Entity configuration for ApplicationSettings
/// </summary>
public class ApplicationSettingsConfiguration : IEntityTypeConfiguration<ApplicationSettings>
{
    public void Configure(EntityTypeBuilder<ApplicationSettings> builder)
    {
        builder.ToTable("ApplicationSettings");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Key)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Value)
            .IsRequired();

        builder.Property(s => s.DataType)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("STRING");

        builder.Property(s => s.Category)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("GENERAL");

        builder.Property(s => s.Description)
            .HasMaxLength(500);

        builder.Property(s => s.IsSystemSetting)
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes
        builder.HasIndex(s => s.Key)
            .IsUnique()
            .HasFilter("[Isdeleted] = 0");

        builder.HasIndex(s => s.Category);

        builder.HasIndex(s => s.IsSystemSetting);
    }
}
