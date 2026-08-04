using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class ChallanConfiguration : IEntityTypeConfiguration<Challan>
{
    public void Configure(EntityTypeBuilder<Challan> builder)
    {
        builder.Property(c => c.Status)
            .HasConversion<string>()
            .IsRequired();
    }
}
