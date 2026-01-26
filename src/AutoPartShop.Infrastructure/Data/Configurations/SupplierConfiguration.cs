using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartsShop.Infrastructure.Data.Configurations
{
    internal class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
    {
        public void Configure(EntityTypeBuilder<Supplier> builder)
        {
            builder.ToTable("Suppliers")
                .HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(x => x.ContactPerson)
                .HasMaxLength(80)
                .IsRequired();

            builder.Property(x => x.Email)
              .HasMaxLength(50)
              .IsRequired();

            builder.Property(x => x.Phone)
            .HasMaxLength(50)
            .IsRequired();

            builder.Property(x => x.Address)
           .HasMaxLength(150)
           .IsRequired();

            builder.Property(x => x.City)
           .HasMaxLength(50)
           .IsRequired();

            builder.Property(x => x.State)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.Country)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.PostalCode)
               .HasMaxLength(50)
               .IsRequired(false);

            builder.Property(x => x.CurrentBalance)
                .HasColumnType("decimal(18,2)")
                .IsRequired(true);

            builder.Property(x => x.Rating);
        }
    }
    
}
