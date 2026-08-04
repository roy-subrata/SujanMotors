namespace AutoPartShop.Infrastructure.Data.Configurations;

using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Entity configuration for NotificationLog
/// </summary>
public class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.Property(n => n.Channel)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(n => n.Status)
            .IsRequired()
            .HasConversion<string>();
    }
}
