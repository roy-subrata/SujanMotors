using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartsShop.Infrastructure.Data.Configurations
{
    internal class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
    {
        public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
        {
            builder.ToTable("PurchaseOrders");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.PONumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(p => p.Status)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(p => p.PaymentStatus)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(p => p.SubTotal).HasColumnType("decimal(18,2)");
            builder.Property(p => p.TaxAmount).HasColumnType("decimal(18,2)");
            builder.Property(p => p.DiscountAmount).HasColumnType("decimal(18,2)");
            builder.Property(p => p.TotalAmount).HasColumnType("decimal(18,2)");
            builder.Property(p => p.PaidAmount).HasColumnType("decimal(18,2)");
            builder.Property(p => p.TaxPercentage).HasColumnType("decimal(5,2)");
            builder.Property(p => p.DiscountPercentage).HasColumnType("decimal(5,2)");

            builder.Property(p => p.Currency)
                .IsRequired()
                .HasMaxLength(3)
                .HasDefaultValue("BDT");

            // Supplier
            builder.HasOne(p => p.Supplier)
                .WithMany()
                .HasForeignKey(p => p.SupplierId);

            // Warehouse
            builder.HasOne(p => p.Warehouse)
                .WithMany()
                .HasForeignKey(p => p.WarehouseId);

            // Line items (1-M)
            builder.HasMany(p => p.LineItems)
                .WithOne(li => li.PurchaseOrder)
                .HasForeignKey(li => li.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Goods Receipts (1-M)
            builder.HasMany(p => p.GoodsReceipts)
                .WithOne(gr => gr.PurchaseOrder)
                .HasForeignKey(gr => gr.PurchaseOrderId);

            // Supplier Payments (1-M)
            builder.HasMany(p => p.SupplierPayments)
                .WithOne(sp => sp.PurchaseOrder)
                .HasForeignKey(sp => sp.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
    {
        public void Configure(EntityTypeBuilder<PurchaseOrderLine> builder)
        {
            builder.ToTable("PurchaseOrderLines");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.UnitPrice)
                .HasColumnType("decimal(18,2)");

            builder.Property(p => p.Description)
                .HasMaxLength(500);

            builder.Property(p => p.LineNumber)
                .IsRequired();

            // PO (many lines → 1 PO)
            builder.HasOne(li => li.PurchaseOrder)
                .WithMany(po => po.LineItems)
                .HasForeignKey(li => li.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Part
            builder.HasOne(li => li.Part)
                .WithMany()
                .HasForeignKey(li => li.PartId)
                .OnDelete(DeleteBehavior.NoAction);

            // Unit
            builder.HasOne(li => li.Unit)
                .WithMany()
                .HasForeignKey(li => li.UnitId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);
        }
    }
    public class GoodsReceiptConfiguration : IEntityTypeConfiguration<GoodsReceipt>
    {
        public void Configure(EntityTypeBuilder<GoodsReceipt> builder)
        {
            builder.ToTable("GoodsReceipts");

            builder.HasKey(g => g.Id);

            builder.Property(g => g.GRNNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(g => g.Status)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(g => g.Notes).HasMaxLength(500);
            builder.Property(g => g.DeliveryReference).HasMaxLength(100);
            builder.Property(g => g.CarrierName).HasMaxLength(100);
            builder.Property(g => g.DriverName).HasMaxLength(100);
            builder.Property(g => g.DeliveryNotes).HasMaxLength(500);

            // Purchase Order (1-M)
            builder.HasOne(g => g.PurchaseOrder)
                .WithMany(po => po.GoodsReceipts)
                .HasForeignKey(g => g.PurchaseOrderId);

            // Warehouse
            builder.HasOne(g => g.Warehouse)
                .WithMany()
                .HasForeignKey(g => g.WarehouseId);

            // Line items
            builder.HasMany(g => g.LineItems)
                .WithOne(li => li.GoodsReceipt)
                .HasForeignKey(li => li.GoodsReceiptId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
    public class GoodsReceiptLineConfiguration : IEntityTypeConfiguration<GoodsReceiptLine>
    {
        public void Configure(EntityTypeBuilder<GoodsReceiptLine> builder)
        {
            builder.ToTable("GoodsReceiptLines");

            builder.HasKey(gl => gl.Id);

            builder.Property(gl => gl.UnitCost)
                .HasColumnType("decimal(18,2)");

            builder.Property(gl => gl.Currency)
                .HasMaxLength(10);

            builder.Property(gl => gl.Condition)
                .HasMaxLength(20);

            builder.Property(gl => gl.SerialNumbers)
                .HasMaxLength(500);

            builder.Property(gl => gl.Notes)
                .HasMaxLength(500);

            // GR (1-M)
            builder.HasOne(gl => gl.GoodsReceipt)
                .WithMany(gr => gr.LineItems)
                .HasForeignKey(gl => gl.GoodsReceiptId);

            // Purchase order line (optional relation)
            builder.HasOne(gl => gl.PurchaseOrderLine)
                .WithMany()
                .HasForeignKey(gl => gl.PurchaseOrderLineId)
                .OnDelete(DeleteBehavior.NoAction);

            // Part
            builder.HasOne(gl => gl.Part)
                .WithMany()






                .HasForeignKey(gl => gl.PartId);
        }
    }


}
