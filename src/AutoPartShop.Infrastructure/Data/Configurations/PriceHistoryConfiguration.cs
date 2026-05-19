using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class PriceHistoryConfiguration : IEntityTypeConfiguration<PriceHistory>
{
    public void Configure(EntityTypeBuilder<PriceHistory> builder)
    {
        builder.HasKey(ph => ph.Id);

        builder.Property(ph => ph.OldPrice)
            .HasPrecision(18, 2);

        builder.Property(ph => ph.NewPrice)
            .HasPrecision(18, 2);

        builder.Property(ph => ph.Reason)
            .IsRequired()
            .HasMaxLength(500);

        // Relationships
        builder.HasOne(ph => ph.Part)
            .WithMany()
            .HasForeignKey(ph => ph.PartId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(ph => ph.PartId);
        builder.HasIndex(ph => ph.EffectiveDate);
    }
}
