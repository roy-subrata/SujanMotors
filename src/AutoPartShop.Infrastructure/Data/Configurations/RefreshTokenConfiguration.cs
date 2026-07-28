using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartShop.Infrastructure.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(t => t.Id);

        // Base64 SHA-256 is always 44 chars; fixed width keeps the unique index narrow.
        builder.Property(t => t.TokenHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(t => t.RevokedReason)
            .HasMaxLength(100);

        builder.Property(t => t.CreatedByIp)
            .HasMaxLength(64); // fits IPv6 plus a port

        // Lookup on presentation is by hash, and a hash collision would be a security bug.
        builder.HasIndex(t => t.TokenHash).IsUnique();

        // Reuse detection revokes a whole family in one statement.
        builder.HasIndex(t => t.FamilyId);

        // "Revoke every session for this user" (password change, deactivation).
        builder.HasIndex(t => t.UserId);

        // Background prune of dead rows.
        builder.HasIndex(t => t.ExpiresAt);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
