using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class ProductEmbeddingConfiguration : IEntityTypeConfiguration<ProductEmbedding>
{
    public void Configure(EntityTypeBuilder<ProductEmbedding> builder)
    {
        builder.ToTable("ProductEmbeddings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PartNumber)
               .HasMaxLength(30)
               .IsRequired();

        builder.Property(x => x.OemNumber)
               .HasMaxLength(100)
               .IsRequired(false);

        // Native SQL Server 2025 vector column. Dimension must match the configured
        // embedding model (text-embedding-3-small = 1536). Changing it requires a migration.
        builder.Property(x => x.Embedding)
               .HasColumnType("vector(1536)")
               .IsRequired();

        builder.Property(x => x.Model)
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(x => x.Dimensions)
               .IsRequired();

        builder.Property(x => x.SourceText)
               .HasColumnType("nvarchar(max)")
               .IsRequired(false);

        builder.HasIndex(x => x.ProductId).IsUnique();

        builder.HasOne(x => x.Product)
               .WithMany()
               .HasForeignKey(x => x.ProductId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
