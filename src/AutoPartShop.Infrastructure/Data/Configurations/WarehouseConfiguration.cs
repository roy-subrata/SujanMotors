

using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace AutoPartShop.Infrastructure.Data.Configurations;
public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouses")
        .HasKey(u => u.Id);

        builder.Property(u => u.Name)
        .IsRequired()
        .HasMaxLength(100);

        builder.Property(u => u.Code)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(u => u.Location)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.City)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.State)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Country)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.PostalCode)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Manager)
            .IsRequired(false)
            .HasMaxLength(100);

        builder.Property(u => u.ManagerEmail)
            .IsRequired(false)
            .HasMaxLength(100);

        builder.Property(u => u.ManagerPhone)
            .IsRequired(false)
            .HasMaxLength(100);

        builder.Property(u => u.StorageCapacity)
             
            .HasColumnType("decimal(18,2)")
            .IsRequired(true);

        builder.Property(u => u.CapacityUnit)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(u => u.Description)
              .IsRequired(false)
              .HasMaxLength(255);

        builder.Property(u => u.IsActive)
               .IsRequired();
    }
}