using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class CustomerDebitNoteConfiguration : IEntityTypeConfiguration<CustomerDebitNote>
{
    public void Configure(EntityTypeBuilder<CustomerDebitNote> builder)
    {
        builder.ToTable("CustomerDebitNotes");
        builder.HasKey(dn => dn.Id);

        builder.Property(dn => dn.DebitNoteNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(dn => dn.TotalAmount).HasColumnType("decimal(18,2)");
        builder.Property(dn => dn.Currency).HasMaxLength(10).HasDefaultValue("BDT");
        builder.Property(dn => dn.Status).IsRequired().HasMaxLength(20);
        builder.Property(dn => dn.Reason).IsRequired().HasMaxLength(500);
        builder.Property(dn => dn.Notes).HasMaxLength(1000);
        builder.Property(dn => dn.IssuedBy).HasMaxLength(100);

        builder.HasOne(dn => dn.Customer)
            .WithMany()
            .HasForeignKey(dn => dn.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(dn => dn.Invoice)
            .WithMany()
            .HasForeignKey(dn => dn.InvoiceId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasIndex(dn => dn.DebitNoteNumber).IsUnique();
        builder.HasIndex(dn => dn.CustomerId);
        builder.HasIndex(dn => dn.Status);
        builder.HasIndex(dn => dn.InvoiceId);
    }
}
