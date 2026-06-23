using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class CustomerVehicleConfiguration : IEntityTypeConfiguration<CustomerVehicle>
{
    public void Configure(EntityTypeBuilder<CustomerVehicle> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.RegistrationNo)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(v => v.VIN)
            .HasMaxLength(50);

        builder.Property(v => v.Make)
            .HasMaxLength(120);

        builder.Property(v => v.Model)
            .HasMaxLength(120);

        builder.Property(v => v.EngineType)
            .HasMaxLength(100);

        builder.Property(v => v.Color)
            .HasMaxLength(50);

        builder.Property(v => v.Notes)
            .HasMaxLength(1000);

        // A vehicle belongs to a customer; cascade so deleting a customer removes their vehicles.
        builder.HasOne(v => v.Customer)
            .WithMany(c => c.Vehicles)
            .HasForeignKey(v => v.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Optional link to the catalog vehicle (parts-fit). Deleting a catalog model must not
        // delete the customer's car.
        builder.HasOne(v => v.CatalogVehicle)
            .WithMany()
            .HasForeignKey(v => v.CatalogVehicleId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasIndex(v => v.CustomerId);
        builder.HasIndex(v => v.RegistrationNo);
    }
}
