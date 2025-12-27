using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class TechnicianConfiguration : IEntityTypeConfiguration<Technician>
{
    public void Configure(EntityTypeBuilder<Technician> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TechnicianCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Phone)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(t => t.Email)
            .HasMaxLength(255);

        builder.Property(t => t.ShopName)
            .HasMaxLength(200);

        builder.Property(t => t.Address)
            .HasMaxLength(500);

        builder.Property(t => t.City)
            .HasMaxLength(100);

        builder.Property(t => t.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(t => t.Notes)
            .HasMaxLength(1000);

        // Relationships
        builder.HasMany(t => t.SalesOrders)
            .WithOne(o => o.Technician)
            .HasForeignKey(o => o.TechnicianId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(t => t.TechnicianCode).IsUnique();
        builder.HasIndex(t => t.Phone);
        builder.HasIndex(t => t.Status);
    }
}
