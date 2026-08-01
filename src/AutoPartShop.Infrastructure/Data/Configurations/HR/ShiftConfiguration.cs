using AutoPartShop.Domain.Entities.HR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations.HR;

public class ShiftConfiguration : IEntityTypeConfiguration<Shift>
{
    public void Configure(EntityTypeBuilder<Shift> builder)
    {
        builder.ToTable("Shifts", schema: "hr");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Notes)
            .HasMaxLength(500);

        builder.HasIndex(s => s.Name)
            .IsUnique()
            .HasFilter("[Isdeleted] = 0");
    }
}
