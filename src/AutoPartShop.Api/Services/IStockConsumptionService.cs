namespace AutoPartShop.Api.Services;

/// <summary>
/// Decrements stock for a single sold line — walking StockLevels (highest-available first),
/// recording a StockMovement per level actually touched, then drawing down StockLots
/// FEFO (soonest expiry first, tie-broken by receiving date) within that level's warehouse
/// and recording a StockLotMovement per lot touched.
///
/// This is the single shared implementation for every channel that sells stock (POS quick
/// sale, in-store handover) — previously each channel had its own copy, which had drifted.
/// </summary>
public interface IStockConsumptionService
{
    /// <summary>
    /// Consumes <paramref name="quantityInBaseUnit"/> units of the given part/variant.
    /// Throws <see cref="InvalidOperationException"/> if the network-wide available quantity
    /// (across all stock levels) is insufficient — callers should stock-check beforehand where
    /// possible, but this is the authoritative, transaction-safe check.
    /// </summary>
    /// <param name="reason">Human-readable audit text, e.g. "Quick Sale INV-2026-0001".</param>
    /// <param name="referenceNumber">Short code for the StockMovement record, e.g. the invoice/SO number.</param>
    /// <param name="sourceType">StockLotMovement.ReferenceType — e.g. "QuickSale".</param>
    /// <param name="actor">CreatedBy/ModifiedBy and the StockMovement approver — cashier username.</param>
    Task ConsumeStockAsync(
        Guid partId,
        Guid? variantId,
        int quantityInBaseUnit,
        Guid salesOrderId,
        string reason,
        string referenceNumber,
        string sourceType,
        string actor,
        CancellationToken cancellationToken = default);
}
