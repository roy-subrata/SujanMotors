namespace AutoPartShop.Api.Services;

/// <summary>
/// The shop's business clock.
///
/// Every timestamp in the database is stored in UTC, but the shop's books close on its own
/// wall clock — so "today's sales" must mean the shop's calendar day, not the UTC one. On a
/// UTC-hosted server the two differ by the whole offset: for Bangladesh (UTC+6) a UTC "today"
/// actually runs 06:00 local to 06:00 the next morning, so every sale rung up before 6 a.m.
/// lands in the wrong day and the daily figures reset mid-morning.
///
/// Configured via:
///   "Shop": { "TzOffsetMinutes": 360 }   // 360 = UTC+6, Bangladesh
///
/// A fixed offset rather than a named time zone is deliberate: Bangladesh observes no DST,
/// the offset needs no tzdata on the container, and it matches the convention already used by
/// ReorderAlerts and the backup scheduler.
/// </summary>
public interface IShopClock
{
    /// <summary>Minutes to add to UTC to reach the shop's wall clock (360 = UTC+6).</summary>
    int OffsetMinutes { get; }

    /// <summary>The current instant, in UTC. Safe to persist.</summary>
    DateTime UtcNow { get; }

    /// <summary>
    /// The shop's current wall-clock time. This is a local reading, not an instant —
    /// use it for display and for deriving calendar dates, never for persistence.
    /// </summary>
    DateTime Now { get; }

    /// <summary>The shop's current calendar date.</summary>
    DateOnly Today { get; }

    /// <summary>Converts a UTC instant to the shop's wall clock.</summary>
    DateTime ToShopTime(DateTime utc);

    /// <summary>
    /// Converts an inclusive range of shop calendar dates into the half-open UTC instant
    /// window <c>[FromUtc, ToUtcExclusive)</c> covering exactly those local days. Time
    /// components on the inputs are ignored — only the calendar date is used.
    /// </summary>
    /// <example>
    /// For 2026-07-28 → 2026-07-28 at UTC+6, returns
    /// [2026-07-27 18:00Z, 2026-07-28 18:00Z).
    /// </example>
    (DateTime FromUtc, DateTime ToUtcExclusive) DayWindowUtc(DateTime localFrom, DateTime localTo);
}

/// <inheritdoc cref="IShopClock"/>
public sealed class ShopClock : IShopClock
{
    // UTC-14 .. UTC+14 — the real-world span of civil offsets.
    private const int MinOffsetMinutes = -840;
    private const int MaxOffsetMinutes = 840;

    private readonly TimeSpan _shift;

    public ShopClock(IConfiguration configuration)
    {
        OffsetMinutes = Math.Clamp(
            configuration.GetValue("Shop:TzOffsetMinutes", 360),
            MinOffsetMinutes,
            MaxOffsetMinutes);

        _shift = TimeSpan.FromMinutes(OffsetMinutes);
    }

    public int OffsetMinutes { get; }

    public DateTime UtcNow => DateTime.UtcNow;

    public DateTime Now => DateTime.UtcNow + _shift;

    public DateOnly Today => DateOnly.FromDateTime(Now);

    public DateTime ToShopTime(DateTime utc) => utc + _shift;

    public (DateTime FromUtc, DateTime ToUtcExclusive) DayWindowUtc(DateTime localFrom, DateTime localTo)
        => (localFrom.Date - _shift, localTo.Date.AddDays(1) - _shift);
}
