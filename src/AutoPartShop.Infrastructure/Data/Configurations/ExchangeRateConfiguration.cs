namespace AutoPartShop.Infrastructure.Data.Configurations;

using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Entity configuration for ExchangeRate
/// </summary>
public class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRate>
{
    public void Configure(EntityTypeBuilder<ExchangeRate> builder)
    {
        builder.ToTable("ExchangeRates");

        builder.HasKey(er => er.Id);

        builder.Property(er => er.FromCurrencyId)
            .IsRequired();

        builder.Property(er => er.ToCurrencyId)
            .IsRequired();

        builder.Property(er => er.Rate)
            .IsRequired()
            .HasPrecision(18, 6);  // More precision for exchange rates

        builder.Property(er => er.EffectiveDate)
            .IsRequired();

        builder.Property(er => er.ExpiryDate)
            .IsRequired(false);

        builder.Property(er => er.Source)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("MANUAL");

        builder.Property(er => er.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(er => er.Notes)
            .HasMaxLength(500);

        // Relationships
        builder.HasOne(er => er.FromCurrency)
            .WithMany()
            .HasForeignKey(er => er.FromCurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(er => er.ToCurrency)
            .WithMany()
            .HasForeignKey(er => er.ToCurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(er => new { er.FromCurrencyId, er.ToCurrencyId, er.EffectiveDate })
            .HasFilter("[Isdeleted] = 0 AND [IsActive] = 1");

        builder.HasIndex(er => er.EffectiveDate);

        builder.HasIndex(er => er.ExpiryDate);

        builder.HasIndex(er => er.IsActive);
    }
}
