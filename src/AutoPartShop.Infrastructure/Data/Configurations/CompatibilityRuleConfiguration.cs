using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class CompatibilityRuleConfiguration : IEntityTypeConfiguration<CompatibilityRule>
{
    public void Configure(EntityTypeBuilder<CompatibilityRule> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceType)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.TargetType)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.HasIndex(x => new { x.SourceType, x.SourceId, x.TargetType, x.TargetId }).IsUnique();
    }
}
