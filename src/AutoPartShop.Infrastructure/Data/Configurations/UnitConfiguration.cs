

using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace AutoPartShop.Infrastructure.Data.Configurations;
public class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.ToTable("Units")
        .HasKey(u => u.Id);

        builder.Property(u => u.Name)
        .IsRequired()
        .HasMaxLength(250);

        builder.Property(u => u.Code)
               .IsRequired()
               .HasMaxLength(250);

        builder.Property(u => u.Symbol)
                .IsRequired()
                .HasMaxLength(5);

        builder.Property(u => u.Description)
              .IsRequired(false)
              .HasMaxLength(255);

        builder.Property(u => u.IsActive)
               .IsRequired();

        builder.Property(u => u.DisplayOrder)
        .IsRequired();


        builder.HasMany(u => u.FromConversions)
        .WithOne(u => u.FromUnit)
        .HasForeignKey(u => u.FromUnitId)
        .OnDelete(DeleteBehavior.NoAction);


        builder.HasMany(u => u.ToConversions)
        .WithOne(u => u.ToUnit)
        .HasForeignKey(u => u.ToUnitId)
        .OnDelete(DeleteBehavior.NoAction);

    }
}