using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class WarrantyClaimEventConfiguration : IEntityTypeConfiguration<WarrantyClaimEvent>
{
    public void Configure(EntityTypeBuilder<WarrantyClaimEvent> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.WarrantyClaimId).IsRequired();
        builder.Property(e => e.EventType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.PartnerType).HasMaxLength(30).IsRequired();
        builder.Property(e => e.PartnerName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.ResponsibleBy).HasMaxLength(100).IsRequired();
        builder.Property(e => e.ReferenceNumber).HasMaxLength(100).IsRequired(false);
        builder.Property(e => e.ExpectedReturnDate).IsRequired(false);
        builder.Property(e => e.EventDate).IsRequired();
        builder.Property(e => e.Notes).HasMaxLength(2000).IsRequired(false);

        builder.HasOne(e => e.WarrantyClaim)
            .WithMany()
            .HasForeignKey(e => e.WarrantyClaimId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(e => e.WarrantyClaimId);
    }
}
