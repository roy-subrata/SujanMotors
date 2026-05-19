using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class CustomerCreditNoteConfiguration : IEntityTypeConfiguration<CustomerCreditNote>
{
    public void Configure(EntityTypeBuilder<CustomerCreditNote> builder)
    {
        builder.ToTable("CustomerCreditNotes");

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

        // Relationship: CustomerCreditNote belongs to Customer
        builder.HasOne(cn => cn.Customer)
            .WithMany()
            .HasForeignKey(cn => cn.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relationship: CustomerCreditNote may belong to a SalesReturn
        builder.HasOne(cn => cn.SalesReturn)
            .WithMany()
            .HasForeignKey(cn => cn.SalesReturnId)
            .OnDelete(DeleteBehavior.SetNull);

        // Relationship: CustomerCreditNote may be applied to an Invoice
        builder.HasOne(cn => cn.Invoice)
            .WithMany()
            .HasForeignKey(cn => cn.InvoiceId)
            .OnDelete(DeleteBehavior.SetNull);

        // Relationship: CustomerCreditNote may be applied to a SalesOrder
        builder.HasOne(cn => cn.SalesOrder)
            .WithMany()
            .HasForeignKey(cn => cn.SalesOrderId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes for common queries
        builder.HasIndex(cn => cn.CreditNoteNumber).IsUnique();
        builder.HasIndex(cn => cn.CustomerId);
        builder.HasIndex(cn => cn.Status);
        builder.HasIndex(cn => cn.SalesReturnId);
        builder.HasIndex(cn => cn.WarrantyClaimId);
    }
}
