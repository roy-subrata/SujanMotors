

using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace AutoPartShop.Infrastructure.Data.Configurations;
public class UnitConversionConfiguration : IEntityTypeConfiguration<UnitConversion>
{
    public void Configure(EntityTypeBuilder<UnitConversion> builder)
    {
        builder.ToTable("UnitConversions")
        .HasKey(u => u.Id);

        builder.Property(x => x.Description)
        .HasMaxLength(255);

        builder.Property(x => x.ConversionFactor)
        .HasColumnType("decimal(18,2)");
    }
}