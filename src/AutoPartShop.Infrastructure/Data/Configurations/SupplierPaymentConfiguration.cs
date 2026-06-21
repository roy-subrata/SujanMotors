using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartsShop.Infrastructure.Data.Configurations
{
    internal class SupplierPaymentConfiguration : IEntityTypeConfiguration<SupplierPayment>
    {
        public void Configure(EntityTypeBuilder<SupplierPayment> builder)
        {
            builder.ToTable("SupplierPayments");

            // Primary Key
            builder.HasKey(x => x.Id);
            builder.Property(x => x.RowVersion).IsRowVersion();

            // Relationships
            builder.HasOne(x => x.Supplier)
                   .WithMany(s => s.SupplierPayments)
                   .HasForeignKey(x => x.SupplierId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.PurchaseOrder)
                   .WithMany(p => p.SupplierPayments)
                   .HasForeignKey(x => x.PurchaseOrderId)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(x => x.GoodsReceipt)
                   .WithMany()
                   .HasForeignKey(x => x.GoodsReceiptId)
                   .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(x => x.PaymentProvider)
                   .WithMany()
                   .HasForeignKey(x => x.PaymentProviderId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.SupplierPaymentAccount)
                   .WithMany()
                   .HasForeignKey(x => x.SupplierPaymentAccountId)
                   .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(x => x.SourceAdvancePayment)
                   .WithMany()
                   .HasForeignKey(x => x.SourceAdvancePaymentId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Required fields
            builder.Property(x => x.TransactionNumber)
                   .HasMaxLength(200)
                   .IsRequired();

            builder.Property(x => x.Currency)
                   .HasMaxLength(10)
                   .HasDefaultValue("USD");

            builder.Property(x => x.PaymentMethod)
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(x => x.Status)
                   .HasMaxLength(50)
                   .HasDefaultValue("PENDING")
                   .IsRequired();

            builder.Property(x => x.ReferenceNumber)
                   .HasMaxLength(200);

            builder.Property(x => x.AuthorizationCode)
                   .HasMaxLength(200);

            builder.Property(x => x.Notes)
                   .HasMaxLength(1000);

            builder.Property(x => x.ProcessedBy)
                   .HasMaxLength(200);

            builder.Property(x => x.ConfirmedBy)
                   .HasMaxLength(200);

            builder.Property(x => x.InvoiceNumber)
                   .HasMaxLength(200);

            builder.Property(x => x.PaymentType)
                .HasConversion<string>()
                .HasMaxLength(25)
                .HasDefaultValue(PaymentType.REGULAR)
                .IsRequired();

            builder.Property(x => x.Description)
                   .HasMaxLength(500);

            // Money precision
            builder.Property(x => x.Amount)
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(x => x.PaymentFee)
                   .HasColumnType("decimal(18,2)");

            builder.Property(x => x.NetAmount)
                   .HasColumnType("decimal(18,2)");

            builder.Property(x => x.RemainingAmount)
                   .HasColumnType("decimal(18,2)")
                   .HasDefaultValue(0);

            // Boolean
            builder.Property(x => x.IsReconciled)
                   .HasDefaultValue(false);

            // Indexes (important for search)
            builder.HasIndex(x => x.SupplierId);
            builder.HasIndex(x => x.TransactionNumber).IsUnique();
            builder.HasIndex(x => x.InvoiceNumber);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.PaymentDate);
            builder.HasIndex(x => x.SourceAdvancePaymentId);
            builder.HasIndex(x => x.PaymentType);
        }
    }
}
