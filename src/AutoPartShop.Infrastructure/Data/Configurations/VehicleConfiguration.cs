using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartsShop.Infrastructure.Data.Configurations
{
    internal class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
    {
        public void Configure(EntityTypeBuilder<Vehicle> builder)
        {
            builder.ToTable("Vehicles")
            .HasKey(x => x.Id);

            builder.Property(x => x.Make)
            .HasMaxLength(120)
            .IsRequired();

            builder.Property(x => x.Model)
            .HasMaxLength(120)
            .IsRequired();

            builder.Property(x => x.EngineType)
            .HasMaxLength(100)
            .IsRequired();

            builder.Property(x => x.EngineType)
                   .HasMaxLength(200)
                   .IsRequired(false);

            builder.Property(x => x.IsActive);

            builder.HasMany(c => c.PartCompatibilities)
                   .WithOne(c => c.Vehicle)
                   .HasForeignKey(c => c.VehicleId)
                   .OnDelete(DeleteBehavior.NoAction);
            //.HasConstraintName("FK_Vehicle_PartVehicleCompatibility");
        }
    }
}
