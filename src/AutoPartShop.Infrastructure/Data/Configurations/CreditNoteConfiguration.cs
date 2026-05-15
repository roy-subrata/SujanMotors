using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class CreditNoteConfiguration : IEntityTypeConfiguration<CreditNote>
{
    public void Configure(EntityTypeBuilder<CreditNote> builder)
    {
        builder.ToTable("CreditNotes");

        builder.HasKey(cn => cn.Id);

        builder.Property(cn => cn.CreditNoteNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(cn => cn.TotalAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(cn => cn.UsedAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(cn => cn.Currency)
            .HasMaxLength(10)
            .HasDefaultValue("USD");

        builder.Property(cn => cn.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(cn => cn.Notes)
            .HasMaxLength(1000);

        builder.Property(cn => cn.IssuedBy)
            .HasMaxLength(100);

        // Relationship: CreditNote belongs to Supplier
        builder.HasOne(cn => cn.Supplier)
            .WithMany()
            .HasForeignKey(cn => cn.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relationship: CreditNote may belong to a PurchaseReturn
        builder.HasOne(cn => cn.PurchaseReturn)
            .WithMany()
            .HasForeignKey(cn => cn.PurchaseReturnId)
            .OnDelete(DeleteBehavior.SetNull);

        // Relationship: CreditNote may be applied to a PurchaseOrder
        builder.HasOne(cn => cn.PurchaseOrder)
            .WithMany()
            .HasForeignKey(cn => cn.PurchaseOrderId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes for common queries
        builder.HasIndex(cn => cn.CreditNoteNumber).IsUnique();
        builder.HasIndex(cn => cn.SupplierId);
        builder.HasIndex(cn => cn.Status);
        builder.HasIndex(cn => cn.PurchaseReturnId);
    }
}
