using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartsShop.Infrastructure.Data.Configurations
{
    internal class ProductSpecificationConfiguration : IEntityTypeConfiguration<ProductSpecification>
    {
        public void Configure(EntityTypeBuilder<ProductSpecification> builder)
        {
            builder.ToTable("ProductSpecifications");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Label)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(x => x.Key)
                   .HasMaxLength(120)
                   .IsRequired();

            builder.Property(x => x.Value)
                   .HasMaxLength(500);

            builder.HasOne(x => x.Part)
                   .WithMany()
                   .HasForeignKey(x => x.PartId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Facet lookups group by Key; suggestion lookups scan Key/Value.
            builder.HasIndex(x => x.PartId);
            builder.HasIndex(x => x.Key);
        }
    }
}
