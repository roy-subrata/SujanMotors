using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class HolidayConfiguration : IEntityTypeConfiguration<Holiday>
{
    public void Configure(EntityTypeBuilder<Holiday> builder)
    {
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Date)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(h => h.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(h => h.Date)
            .IsUnique()
            .HasFilter("[Isdeleted] = 0");
    }
}
