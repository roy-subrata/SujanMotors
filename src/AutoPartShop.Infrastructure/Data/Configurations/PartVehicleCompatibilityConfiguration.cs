using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartsShop.Infrastructure.Data.Configurations
{
    internal class PartVehicleCompatibilityConfiguration : IEntityTypeConfiguration<PartVehicleCompatibility>
    {
        public void Configure(EntityTypeBuilder<PartVehicleCompatibility> builder)
        {
            builder.ToTable("PartVehicles")
              .HasKey(x => x.Id);

            builder.Property(x => x.Notes)
             .HasMaxLength(200)
             .IsRequired(false);

            //  builder.HasOne(x=>x.Part)
            //  .WithMany(x=>x.VehicleCompatibilities)
            //  .HasForeignKey(x=>x.PartId)
            //  .OnDelete(DeleteBehavior.NoAction);

        }
    }
}
