namespace AutoPartShop.Infrastructure.Data.Configurations;

using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Entity configuration for Currency
/// </summary>
public class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.ToTable("Currencies");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Symbol)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(c => c.DecimalPlaces)
            .IsRequired()
            .HasDefaultValue(2);

        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(c => c.IsBaseCurrency)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(c => c.DisplayOrder)
            .IsRequired()
            .HasDefaultValue(0);

        // Indexes
        builder.HasIndex(c => c.Code)
            .IsUnique()
            .HasFilter("[Isdeleted] = 0");

        builder.HasIndex(c => c.IsBaseCurrency)
            .HasFilter("[IsBaseCurrency] = 1 AND [Isdeleted] = 0");

        builder.HasIndex(c => c.IsActive);

        builder.HasIndex(c => c.DisplayOrder);
    }
}
