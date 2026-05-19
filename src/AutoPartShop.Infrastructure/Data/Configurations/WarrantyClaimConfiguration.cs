using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class WarrantyClaimConfiguration : IEntityTypeConfiguration<WarrantyClaim>
{
    public void Configure(EntityTypeBuilder<WarrantyClaim> builder)
    {
        builder.HasKey(wc => wc.Id);

        builder.Property(wc => wc.ClaimNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(wc => wc.ClaimNumber)
            .IsUnique();

        builder.Property(wc => wc.WarrantyRegistrationId)
            .IsRequired();

        builder.Property(wc => wc.CustomerId)
            .IsRequired();

        builder.Property(wc => wc.TechnicianId)
            .IsRequired(false);

        builder.Property(wc => wc.ClaimDate)
            .IsRequired();

        builder.Property(wc => wc.IssueDescription)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(wc => wc.ServiceType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(wc => wc.Status)
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue("PENDING");

        builder.Property(wc => wc.RejectionReason)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(wc => wc.RejectedDate)
            .IsRequired(false);

        builder.Property(wc => wc.ApprovedDate)
            .IsRequired(false);

        builder.Property(wc => wc.ApprovedBy)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(wc => wc.ServiceStartDate)
            .IsRequired(false);

        builder.Property(wc => wc.ServiceCompletedDate)
            .IsRequired(false);

        builder.Property(wc => wc.ServiceCost)
            .HasColumnType("decimal(18,2)")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(wc => wc.ServiceCostCurrency)
            .HasMaxLength(3)
            .IsRequired()
            .HasDefaultValue("BDT");

        builder.Property(wc => wc.ServiceNotes)
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.Property(wc => wc.ResolutionDetails)
            .HasMaxLength(2000)
            .IsRequired(false);

        // Foreign key relationships
        builder.HasOne(wc => wc.WarrantyRegistration)
            .WithMany()
            .HasForeignKey(wc => wc.WarrantyRegistrationId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(wc => wc.Customer)
            .WithMany()
            .HasForeignKey(wc => wc.CustomerId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(wc => wc.Technician)
            .WithMany()
            .HasForeignKey(wc => wc.TechnicianId)
            .OnDelete(DeleteBehavior.NoAction);

        // Indexes for better query performance
        builder.HasIndex(wc => wc.WarrantyRegistrationId);
        builder.HasIndex(wc => wc.CustomerId);
        builder.HasIndex(wc => wc.TechnicianId);
        builder.HasIndex(wc => wc.Status);
        builder.HasIndex(wc => wc.ClaimDate);
        builder.HasIndex(wc => wc.ServiceType);
    }
}
