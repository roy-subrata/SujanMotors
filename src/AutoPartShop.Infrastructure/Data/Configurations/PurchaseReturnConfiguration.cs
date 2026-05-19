using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class PurchaseReturnConfiguration : IEntityTypeConfiguration<PurchaseReturn>
{
    public void Configure(EntityTypeBuilder<PurchaseReturn> builder)
    {
        builder.HasKey(pr => pr.Id);

        builder.Property(pr => pr.ReturnNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(pr => pr.RefundAmount)
            .HasPrecision(18, 2);

        builder.Property(pr => pr.CreditNoteAmount)
            .HasPrecision(18, 2);

        builder.Property(pr => pr.Status)
            .IsRequired()
            .HasMaxLength(20);

        // Settlement tracking fields
        builder.Property(pr => pr.SettlementStatus)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("PENDING");

        builder.Property(pr => pr.SettledAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(pr => pr.SettlementMethod)
            .HasMaxLength(50);

        builder.Property(pr => pr.SettlementNotes)
            .HasMaxLength(500);

        // Credit note relationship
        builder.Property(pr => pr.CreditNoteId);

        builder.HasOne(pr => pr.CreditNote)
            .WithMany()
            .HasForeignKey(pr => pr.CreditNoteId)
            .OnDelete(DeleteBehavior.SetNull);

        // Relationships - Use NoAction to avoid cascade paths
        builder.HasOne(pr => pr.PurchaseOrder)
            .WithMany()
            .HasForeignKey(pr => pr.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pr => pr.Supplier)
            .WithMany()
            .HasForeignKey(pr => pr.SupplierId)
            .OnDelete(DeleteBehavior.Restrict); // This is the key fix for the error

        builder.HasMany(pr => pr.LineItems)
            .WithOne(li => li.PurchaseReturn)
            .HasForeignKey(li => li.PurchaseReturnId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(pr => pr.ReturnNumber).IsUnique();
        builder.HasIndex(pr => pr.PurchaseOrderId);
        builder.HasIndex(pr => pr.SupplierId);
        builder.HasIndex(pr => pr.Status);
    }
}
