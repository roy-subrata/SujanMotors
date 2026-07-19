using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class TillSessionConfiguration : IEntityTypeConfiguration<TillSession>
{
    public void Configure(EntityTypeBuilder<TillSession> builder)
    {
        builder.ToTable("TillSessions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.CashierUsername).IsRequired().HasMaxLength(100);
        builder.Property(t => t.TerminalLabel).IsRequired().HasMaxLength(50);
        builder.Property(t => t.ShiftLabel).HasMaxLength(50);
        builder.Property(t => t.Status).IsRequired().HasMaxLength(20);
        builder.Property(t => t.Notes).HasMaxLength(1000);

        builder.Property(t => t.OpeningFloat).HasPrecision(18, 2);
        builder.Property(t => t.ClosingCountedAmount).HasPrecision(18, 2);
        builder.Property(t => t.CashSalesTotal).HasPrecision(18, 2);
        builder.Property(t => t.CashRefundsTotal).HasPrecision(18, 2);
        builder.Property(t => t.CashDropsTotal).HasPrecision(18, 2);
        builder.Property(t => t.ExpectedAmount).HasPrecision(18, 2);
        builder.Property(t => t.OverShortAmount).HasPrecision(18, 2);

        builder.HasOne(t => t.Cashier)
            .WithMany()
            .HasForeignKey(t => t.CashierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.CashDrops)
            .WithOne(d => d.TillSession)
            .HasForeignKey(d => d.TillSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.CashierId);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.OpenedAt);
        // A cashier can have at most one OPEN session — enforced in the application layer (a
        // filtered unique index would need a computed/persisted status-is-open column to express
        // "unique while OPEN" portably, which is more than this needs).
    }
}

public class TillCashDropConfiguration : IEntityTypeConfiguration<TillCashDrop>
{
    public void Configure(EntityTypeBuilder<TillCashDrop> builder)
    {
        builder.ToTable("TillCashDrops");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Amount).HasPrecision(18, 2);
        builder.Property(d => d.Notes).HasMaxLength(500);

        builder.HasIndex(d => d.TillSessionId);
    }
}
