using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartsShop.Infrastructure.Data.Configurations
{
    internal class DailyExpenseConfiguration : IEntityTypeConfiguration<DailyExpense>
    {
        public void Configure(EntityTypeBuilder<DailyExpense> builder)
        {
            builder.ToTable("DailyExpenses");

            // Primary Key
            builder.HasKey(x => x.Id);

            // Required fields
            builder.Property(x => x.Category)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(x => x.Description)
                   .HasMaxLength(500)
                   .IsRequired();

            builder.Property(x => x.PaymentMethod)
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(x => x.ReferenceNumber)
                   .HasMaxLength(200);

            builder.Property(x => x.Notes)
                   .HasMaxLength(1000);

            builder.Property(x => x.VendorName)
                   .HasMaxLength(200);

            builder.Property(x => x.RecurrencePattern)
                   .HasMaxLength(50);

            // Money precision
            builder.Property(x => x.Amount)
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(x => x.Currency)
                   .HasMaxLength(3)
                   .IsRequired()
                   .HasDefaultValue("BDT");

            // Boolean
            builder.Property(x => x.IsRecurring)
                   .HasDefaultValue(false);

            // Indexes (important for search and filtering)
            builder.HasIndex(x => x.ExpenseDate);
            builder.HasIndex(x => x.Category);
            builder.HasIndex(x => x.VendorName);
            builder.HasIndex(x => x.IsRecurring);
        }
    }
}
