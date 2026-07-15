using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.StorageKey)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.OriginalFileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.ContentType)
            .HasMaxLength(127)
            .IsRequired();

        builder.Property(x => x.Kind)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.OwnerType)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(x => x.StorageKey)
            .IsUnique();

        builder.HasIndex(x => new { x.OwnerType, x.OwnerId });
    }
}
