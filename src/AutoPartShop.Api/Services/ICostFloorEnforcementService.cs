using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Api.Services;

/// <summary>
/// Per-line data needed to re-check a line's cost floor after a cart-level discount is known.
/// Deliberately entity-agnostic (SalesOrderLine and QuotationLine both fit this shape) so both
/// SalesOrderController and QuotationController can share one enforcement path. LineTotalPrice/
/// Quantity/QuantityInBaseUnit drive the proportional distribution; NetUnitPriceInOrderCurrency is
/// the line's OWN net price (before any cart-level discount), still in the order's currency —
/// conversion to the shop's base currency happens at enforcement time, once the cart discount's
/// per-line share is known too. MinMarginPercent is resolved per-line (its part's category may
/// carry its own override) rather than shared across the whole cart.
/// </summary>
public readonly record struct LineCostFloorCheck(
    string PartName,
    decimal LineTotalPrice,
    int Quantity,
    int QuantityInBaseUnit,
    decimal NetUnitPriceInOrderCurrency,
    decimal CostPerBaseUnit,
    decimal MinMarginPercent);

/// <summary>
/// Everything a request's line-building needs from ICostFloorEnforcementService, resolved ONCE per
/// request (batched category margins, one FX rate, one approval-token lookup) instead of per line —
/// see ICostFloorEnforcementService.PrepareContextAsync. Shared across SalesOrderController,
/// QuotationController, and QuotesController so this resolution logic exists exactly once.
/// </summary>
public sealed record CostFloorContext(
    IReadOnlyDictionary<Guid, Guid> CategoryIdByPart,
    IReadOnlyDictionary<Guid, decimal> MinMarginByCategory,
    decimal RateToBase,
    PriceOverrideApproval? Approval)
{
    public decimal MinMarginFor(Guid partId) =>
        CategoryIdByPart.TryGetValue(partId, out var categoryId) && MinMarginByCategory.TryGetValue(categoryId, out var margin)
            ? margin
            : 0m;
}

/// <summary>
/// Enforces retail price-policy rules wherever a SalesOrderLine's final price is determined — quick
/// sale, the regular Sales Order create/update flow, quotation create/convert, and standalone
/// quotes. Centralized so every path that can produce a real, confirmed sale enforces the same
/// rules the same way:
///   - Floor: net price must stay at/above cost × (1 + category or shop-wide min margin %).
///   - Ceiling: gross (pre-discount) price must stay at/below the part's MRP.
/// Both are hard blocks UNLESS a valid PriceOverrideApproval token accompanies the request — a
/// manager must authenticate for that specific sale via PricingController's request-override
/// endpoint; there is no role-based silent bypass.
/// </summary>
public interface ICostFloorEnforcementService
{
    /// <summary>
    /// The minimum margin (%) a line's net price must stay above its resolved cost, resolved per
    /// category (Category.MinMarginPercentOverride when set, else the shop-wide
    /// SALES_MIN_MARGIN_PERCENT setting). Batched: pass every distinct CategoryId in the request
    /// once, not one call per line.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, decimal>> GetMinMarginPercentsAsync(
        IEnumerable<Guid> categoryIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The rate that converts an amount in orderCurrency to the shop's base currency on
    /// effectiveDate (1m when orderCurrency already is the base currency). Callers resolve this
    /// ONCE per request — every line in the same order shares the same currency and date — and pass
    /// the result into EnforceLine/EnforceCeiling/EnforceCartDiscount instead of each doing its own
    /// FX lookup.
    /// </summary>
    Task<decimal> GetRateToBaseAsync(
        string orderCurrency,
        DateTime effectiveDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves and validates a price-override approval token (must exist, be unexpired, and not
    /// already consumed). Returns null when no token was supplied, or it's invalid/expired/used —
    /// callers then get normal (no-override) enforcement. Does NOT mark the token consumed; that
    /// only happens once the caller confirms it was actually needed (see EnforceLine's return value)
    /// and calls MarkApprovalConsumedAsync.
    /// </summary>
    Task<PriceOverrideApproval?> ResolveApprovalAsync(Guid? approvalToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an approval consumed against the given sale and persists it, writing a
    /// PRICE_OVERRIDE_APPROVED AuditLog row alongside — call at most once per request, only if at
    /// least one Enforce* call actually needed it (returned true).
    /// </summary>
    Task MarkApprovalConsumedAsync(PriceOverrideApproval approval, string salesOrderNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves everything a request's line-building needs in one shot: each distinct part's
    /// category, the margin that applies per category, the FX rate for the order's currency, and
    /// the approval token (if any). Call once per request, pass the result down to each line.
    /// </summary>
    Task<CostFloorContext> PrepareContextAsync(
        IEnumerable<Guid> partIds,
        string currency,
        DateTime effectiveDate,
        Guid? approvalToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a blocked price-override attempt (no valid approval was supplied) as an AuditLog row.
    /// Call this from the controller's catch block AFTER the failed transaction has rolled back —
    /// never from inside EnforceLine/EnforceCeiling themselves, since a row added there would be
    /// rolled back with everything else. Clears the DbContext's change tracker first, since after a
    /// rollback it may still hold the failed sale's now-orphaned tracked entities.
    /// </summary>
    Task LogBlockedAttemptAsync(PriceOverrideRequiredException ex, string performedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Floor check: rejects a line whose net price (in the order's currency) converts to below
    /// cost × (1 + minMarginPercent) in the shop's base currency — UNLESS approval is a valid,
    /// unexpired, unconsumed token, in which case it's allowed and this returns true. A line with
    /// no known cost (costPerBaseUnit &lt;= 0) is never flagged either way. Throws
    /// PriceOverrideRequiredException (not a plain ArgumentException) when blocked, so callers can
    /// tell the frontend to prompt for approval rather than just show a generic error.
    /// </summary>
    bool EnforceLine(
        string partName,
        decimal netUnitPriceInOrderCurrency,
        decimal costPerBaseUnit,
        decimal minMarginPercent,
        decimal rateToBase,
        PriceOverrideApproval? approval);

    /// <summary>
    /// Ceiling check: rejects a line whose GROSS (pre-discount) price converts to above the part's
    /// MRP — same approval semantics as EnforceLine. Checked on the entered/resolved price, not the
    /// net-of-discount price (discounting down from MRP is normal; entering a price above MRP in
    /// the first place is what this blocks).
    /// </summary>
    bool EnforceCeiling(
        string partName,
        decimal grossUnitPriceInOrderCurrency,
        decimal mrp,
        decimal rateToBase,
        PriceOverrideApproval? approval);

    /// <summary>
    /// A cart-level discount (manual amount, promo code, auto threshold, or a percentage baked into
    /// order.DiscountAmount) isn't tied to any one line, so a line whose own discount passed
    /// EnforceLine in isolation can still end up below cost once the cart discount is spread
    /// across the order. Distributes totalCartDiscount proportionally by each line's share of
    /// subTotal and re-checks every line (against its own MinMarginPercent) with that share
    /// subtracted too. Returns true if approval was used for at least one line.
    /// </summary>
    bool EnforceCartDiscount(
        IReadOnlyList<LineCostFloorCheck> lineChecks,
        decimal subTotal,
        decimal totalCartDiscount,
        decimal rateToBase,
        PriceOverrideApproval? approval);
}
