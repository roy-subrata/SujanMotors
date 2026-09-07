namespace AutoPartShop.Api.Services;

public interface ICostResolutionService
{
    /// <summary>
    /// Resolves the authoritative per-base-unit COST for a single part/variant — the FIFO
    /// (oldest-received) sellable stock lot's cost, falling back to the variant's or product's
    /// catalogue CostPrice. Returns 0 when no cost is known.
    /// </summary>
    Task<decimal> ResolveCostPerBaseUnitAsync(
        Guid partId,
        Guid? productVariantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch-resolves per-base-unit cost for multiple part/variant pairs in one pass (avoids N+1
    /// queries). Returns a dictionary keyed by (PartId, ProductVariantId) — missing/zero-cost
    /// entries resolve to 0.
    /// </summary>
    Task<IDictionary<(Guid PartId, Guid? ProductVariantId), decimal>> ResolveCostsPerBaseUnitAsync(
        IList<(Guid PartId, Guid? ProductVariantId)> items,
        CancellationToken cancellationToken = default);
}
