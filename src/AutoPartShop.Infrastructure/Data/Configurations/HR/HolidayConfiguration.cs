using AutoPartShop.Domain.Entities.HR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations.HR;

public class HolidayConfiguration : IEntityTypeConfiguration<Holiday>
{
    public void Configure(EntityTypeBuilder<Holiday> builder)
    {
        builder.ToTable("Holidays", schema: "hr");

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
