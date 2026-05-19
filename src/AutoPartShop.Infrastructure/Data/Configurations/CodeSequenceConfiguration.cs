using AutoPartsShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartsShop.Infrastructure.Data.Configurations
{
    internal class CodeSequenceConfiguration : IEntityTypeConfiguration<CodeSequence>
    {
        public void Configure(EntityTypeBuilder<CodeSequence> builder)
        {
            // Table name
            builder.ToTable("CodeSequences");

            // Primary key
            builder.HasKey(cs => cs.Prefix);

            // Prefix column configuration
            builder.Property(cs => cs.Prefix)
                .IsRequired()
                .HasMaxLength(20);

            // LastNumber column configuration
            builder.Property(cs => cs.LastNumber)
                .IsRequired()
                .HasDefaultValue(0);

            builder.HasIndex(cs => cs.LastNumber);

        }
    }
}
