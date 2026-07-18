using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class ProformaInvoiceConfiguration : IEntityTypeConfiguration<ProformaInvoice>
{
    public void Configure(EntityTypeBuilder<ProformaInvoice> builder)
    {
        builder.ToTable("ProformaInvoices");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.ProformaNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Status).IsRequired().HasMaxLength(20);
        builder.Property(p => p.IssuedBy).HasMaxLength(100);
        builder.Property(p => p.Notes).HasMaxLength(1000);

        // A SalesOrder can have several proforma invoices over time (e.g. reissued after a price
        // change), so this is many-to-one, not one-to-one.
        builder.HasOne(p => p.SalesOrder)
            .WithMany()
            .HasForeignKey(p => p.SalesOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.ProformaNumber).IsUnique();
        builder.HasIndex(p => p.SalesOrderId);
        builder.HasIndex(p => p.Status);
    }
}
