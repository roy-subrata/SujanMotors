using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class CartReservationConfiguration : IEntityTypeConfiguration<CartReservation>
{
    public void Configure(EntityTypeBuilder<CartReservation> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.RowVersion).IsRowVersion();

        builder.Property(r => r.SessionId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.Quantity)
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.Property(r => r.ExpiresAt)
            .IsRequired();

        builder.Property(r => r.IsReleased)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasOne(r => r.Part)
            .WithMany()
            .HasForeignKey(r => r.PartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.SessionId);
        builder.HasIndex(r => r.PartId);
        builder.HasIndex(r => r.ExpiresAt);
        builder.HasIndex(r => new { r.SessionId, r.PartId, r.ProductVariantId });
    }
}
