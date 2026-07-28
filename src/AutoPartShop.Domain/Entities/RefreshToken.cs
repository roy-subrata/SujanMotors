namespace AutoPartShop.Domain.Entities;

/// <summary>
/// A persisted, single-use refresh token.
///
/// <para>Only a SHA-256 hash of the token is stored: the raw value is returned to the client
/// once at issue time and never again, so a database leak cannot be replayed as a session.</para>
///
/// <para><b>Rotation and families.</b> Each refresh consumes the presented token and issues a
/// new one carrying the same <see cref="FamilyId"/>. A family therefore represents one login
/// session across all its rotations. If an already-consumed token is presented again, the
/// token was replayed — either by an attacker who stole it or by the legitimate client after
/// an attacker used it — and the entire family is revoked, forcing a fresh login. This is the
/// standard refresh-token-rotation reuse detection.</para>
/// </summary>
public class RefreshToken
{
    public Guid Id { get; private set; }

    /// <summary>Owning user (<see cref="ApplicationUser"/>).</summary>
    public Guid UserId { get; private set; }

    /// <summary>Base64 SHA-256 of the raw token. The raw value is never persisted.</summary>
    public string TokenHash { get; private set; } = null!;

    /// <summary>Groups every rotation of a single login session, for reuse detection.</summary>
    public Guid FamilyId { get; private set; }

    /// <summary>Absolute expiry. Rotation never extends it — a session dies at its family's expiry.</summary>
    public DateTime ExpiresAt { get; private set; }

    public DateTime CreatedAt { get; private set; }

    /// <summary>Client IP at issue time, for audit. Best-effort; may be a proxy address.</summary>
    public string? CreatedByIp { get; private set; }

    /// <summary>Set when this token was exchanged for a new one. A used token is spent forever.</summary>
    public DateTime? UsedAt { get; private set; }

    public DateTime? RevokedAt { get; private set; }

    /// <summary>Why the token was revoked — "logout", "password-change", "reuse-detected", ...</summary>
    public string? RevokedReason { get; private set; }

    private RefreshToken() { }

    /// <summary>
    /// Creates a token record. <paramref name="tokenHash"/> must already be hashed — this type
    /// never sees the raw value.
    /// </summary>
    public static RefreshToken Create(
        Guid userId,
        string tokenHash,
        Guid familyId,
        DateTime expiresAtUtc,
        string? createdByIp)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty", nameof(userId));

        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("TokenHash cannot be empty", nameof(tokenHash));

        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            FamilyId = familyId == Guid.Empty ? Guid.NewGuid() : familyId,
            ExpiresAt = expiresAtUtc,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = createdByIp
        };
    }

    /// <summary>Still redeemable: not spent, not revoked, not past expiry.</summary>
    public bool IsActive(DateTime utcNow) => UsedAt is null && RevokedAt is null && ExpiresAt > utcNow;

    /// <summary>Marks this token spent as part of a rotation. Idempotent-safe: only the first call sticks.</summary>
    public void MarkUsed(DateTime utcNow) => UsedAt ??= utcNow;

    /// <summary>Revokes the token. Keeps the first reason if already revoked.</summary>
    public void Revoke(DateTime utcNow, string reason)
    {
        if (RevokedAt is not null)
            return;

        RevokedAt = utcNow;
        RevokedReason = reason;
    }
}
