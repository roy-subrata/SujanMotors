using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class StockLotMovementConfiguration : IEntityTypeConfiguration<StockLotMovement>
{
    public void Configure(EntityTypeBuilder<StockLotMovement> builder)
    {
        builder.HasKey(slm => slm.Id);

        builder.Property(slm => slm.CostAtMovement)
            .HasPrecision(18, 2);

        builder.Property(slm => slm.CostAtMovementInBaseUnit)
            .HasPrecision(18, 2);

        builder.Property(slm => slm.MovementType)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(slm => slm.ReferenceType)
            .HasMaxLength(50);

        // Relationships
        builder.HasOne(slm => slm.StockLot)
            .WithMany(sl => sl.Movements)
            .HasForeignKey(slm => slm.StockLotId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(slm => slm.Unit)
            .WithMany()
            .HasForeignKey(slm => slm.UnitId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // Indexes
        builder.HasIndex(slm => slm.StockLotId);
        builder.HasIndex(slm => slm.MovementDate);
        builder.HasIndex(slm => slm.MovementType);
    }
}
