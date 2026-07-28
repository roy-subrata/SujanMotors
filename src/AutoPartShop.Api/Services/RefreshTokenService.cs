using System.Security.Cryptography;
using System.Text;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Services;

/// <summary>Outcome of exchanging a refresh token for a new one.</summary>
public sealed record RefreshResult(
    bool Succeeded,
    ApplicationUser? User,
    string? RefreshToken,
    DateTime? ExpiresAt,
    string? FailureReason)
{
    public static RefreshResult Fail(string reason) => new(false, null, null, null, reason);

    public static RefreshResult Ok(ApplicationUser user, string refreshToken, DateTime expiresAt) =>
        new(true, user, refreshToken, expiresAt, null);
}

/// <summary>
/// Issues, rotates and revokes persisted refresh tokens.
///
/// <para>The access JWT stays short-lived and stateless; durability of a session lives here
/// instead, so a session can actually be ended. Tokens are single-use: every refresh consumes
/// the presented token and issues a successor in the same family
/// (see <see cref="Domain.Entities.RefreshToken"/> for the reuse-detection rationale).</para>
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>Starts a new session and returns the raw refresh token (shown to the caller once).</summary>
    Task<(string Token, DateTime ExpiresAt)> IssueAsync(ApplicationUser user, string? ip, CancellationToken ct = default);

    /// <summary>
    /// Consumes <paramref name="rawToken"/> and issues its successor. Fails closed on anything
    /// suspicious, revoking the family when a spent token is replayed.
    /// </summary>
    Task<RefreshResult> RotateAsync(string rawToken, string? ip, CancellationToken ct = default);

    /// <summary>Ends the single session identified by <paramref name="rawToken"/>. Silent when unknown.</summary>
    Task RevokeAsync(string rawToken, string reason, CancellationToken ct = default);

    /// <summary>Ends every session for a user — password change, deactivation, "sign out everywhere".</summary>
    Task RevokeAllForUserAsync(Guid userId, string reason, CancellationToken ct = default);
}

/// <inheritdoc cref="IRefreshTokenService"/>
public sealed class RefreshTokenService : IRefreshTokenService
{
    // 256 bits of CSPRNG output — far beyond guessing, and the value never needs to be parsed.
    private const int TokenBytes = 32;

    private readonly AutoPartDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RefreshTokenService> _logger;

    public RefreshTokenService(
        AutoPartDbContext db,
        IConfiguration configuration,
        ILogger<RefreshTokenService> logger)
    {
        _db = db;
        _configuration = configuration;
        _logger = logger;
    }

    private int ExpiryDays =>
        Math.Clamp(_configuration.GetValue("JwtSettings:RefreshTokenExpiryInDays", 7), 1, 365);

    public async Task<(string Token, DateTime ExpiresAt)> IssueAsync(
        ApplicationUser user, string? ip, CancellationToken ct = default)
    {
        // A fresh login starts a fresh family; nothing links it to prior sessions.
        var (token, expiresAt) = await IssueInFamilyAsync(user.Id, Guid.NewGuid(), null, ip, ct);
        await _db.SaveChangesAsync(ct);
        return (token, expiresAt);
    }

    public async Task<RefreshResult> RotateAsync(string rawToken, string? ip, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            return RefreshResult.Fail("missing");

        var now = DateTime.UtcNow;
        var hash = Hash(rawToken);

        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (existing is null)
            return RefreshResult.Fail("unknown");

        // Replay of a spent or revoked token: either it was stolen and already used, or the
        // legitimate client is retrying after a thief beat it here. Either way the family is
        // compromised — kill every session in it and make the user log in again.
        if (existing.UsedAt is not null || existing.RevokedAt is not null)
        {
            await RevokeFamilyAsync(existing.FamilyId, "reuse-detected", now, ct);
            await _db.SaveChangesAsync(ct);

            _logger.LogWarning(
                "Refresh token reuse detected for user {UserId}; revoked family {FamilyId}",
                existing.UserId, existing.FamilyId);

            return RefreshResult.Fail("reuse-detected");
        }

        if (existing.ExpiresAt <= now)
            return RefreshResult.Fail("expired");

        // Re-check the account on every rotation, so deactivating a user actually ends their
        // session rather than waiting for the refresh token's absolute expiry.
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == existing.UserId, ct);
        if (user is null || !user.IsActive)
        {
            await RevokeFamilyAsync(existing.FamilyId, "user-inactive", now, ct);
            await _db.SaveChangesAsync(ct);
            return RefreshResult.Fail("user-inactive");
        }

        existing.MarkUsed(now);

        // The successor inherits the family's absolute expiry: rotating cannot extend a session
        // indefinitely, so a stolen token is still bounded by the original login.
        var (token, expiresAt) = await IssueInFamilyAsync(
            user.Id, existing.FamilyId, existing.ExpiresAt, ip, ct);

        await _db.SaveChangesAsync(ct);

        return RefreshResult.Ok(user, token, expiresAt);
    }

    public async Task RevokeAsync(string rawToken, string reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            return;

        var hash = Hash(rawToken);
        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (existing is null)
            return;

        // Revoke the family, not just this token: logging out must end the session, and the
        // client may hold a successor we don't know about.
        await RevokeFamilyAsync(existing.FamilyId, reason, DateTime.UtcNow, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RevokeAllForUserAsync(Guid userId, string reason, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var live = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var token in live)
            token.Revoke(now, reason);

        await _db.SaveChangesAsync(ct);
    }

    // ── internals ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Adds a token to the change tracker without saving, so a rotation persists the spent
    /// token and its successor in one transaction.
    /// </summary>
    private async Task<(string Token, DateTime ExpiresAt)> IssueInFamilyAsync(
        Guid userId, Guid familyId, DateTime? inheritedExpiry, string? ip, CancellationToken ct)
    {
        var raw = GenerateToken();

        // SQL Server datetime2 carries no offset, so EF hands back DateTimeKind.Unspecified.
        // Left alone, an inherited expiry would serialize without the "Z" designator and a
        // browser would read it as local time — six hours out for this shop. Pin it to UTC so
        // the field means the same thing on every code path.
        var expiresAt = inheritedExpiry is { } inherited
            ? DateTime.SpecifyKind(inherited, DateTimeKind.Utc)
            : DateTime.UtcNow.AddDays(ExpiryDays);

        _db.RefreshTokens.Add(RefreshToken.Create(userId, Hash(raw), familyId, expiresAt, ip));

        await PruneAsync(userId, ct);

        return (raw, expiresAt);
    }

    private async Task RevokeFamilyAsync(Guid familyId, string reason, DateTime now, CancellationToken ct)
    {
        var family = await _db.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var token in family)
            token.Revoke(now, reason);
    }

    /// <summary>
    /// Drops this user's long-dead rows so the table doesn't grow without bound. Expired-and-
    /// revoked tokens have no forensic value left; recent ones are kept for reuse detection.
    /// </summary>
    private async Task PruneAsync(Guid userId, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddDays(-ExpiryDays);

        await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.ExpiresAt < cutoff)
            .ExecuteDeleteAsync(ct);
    }

    private static string GenerateToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(TokenBytes));

    private static string Hash(string rawToken) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
