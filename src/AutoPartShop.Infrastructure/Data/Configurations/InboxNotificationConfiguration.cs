namespace AutoPartShop.Infrastructure.Data.Configurations;

using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Entity configuration for InboxNotification
/// </summary>
public class InboxNotificationConfiguration : IEntityTypeConfiguration<InboxNotification>
{
    public void Configure(EntityTypeBuilder<InboxNotification> builder)
    {
        builder.Property(n => n.Type)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(n => n.Message)
            .HasMaxLength(1000);

        builder.HasIndex(n => n.Type);
        builder.HasIndex(n => new { n.IsRead, n.CreatedDate });
    }
}
