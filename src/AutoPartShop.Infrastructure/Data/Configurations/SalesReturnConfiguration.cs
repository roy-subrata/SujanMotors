using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class SalesReturnConfiguration : IEntityTypeConfiguration<SalesReturn>
{
    public void Configure(EntityTypeBuilder<SalesReturn> builder)
    {
        builder.HasKey(sr => sr.Id);

        builder.Property(sr => sr.ReturnNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(sr => sr.RefundAmount)
            .HasPrecision(18, 2);

        builder.Property(sr => sr.Status)
            .IsRequired()
            .HasMaxLength(20);

        // Relationships
        builder.HasOne(sr => sr.SalesOrder)
            .WithMany()
            .HasForeignKey(sr => sr.SalesOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(sr => sr.LineItems)
            .WithOne(li => li.SalesReturn)
            .HasForeignKey(li => li.SalesReturnId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(sr => sr.ReturnNumber).IsUnique();
        builder.HasIndex(sr => sr.SalesOrderId);
        builder.HasIndex(sr => sr.Status);
        builder.HasIndex(sr => sr.ReturnDate);
    }
}
