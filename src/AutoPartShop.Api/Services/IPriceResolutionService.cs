namespace AutoPartShop.Api.Services;

public record PriceResolutionResult(
    decimal SellingPrice,
    string Currency,
    string Source,        // VARIANT_HISTORY | PRODUCT_HISTORY
    Guid? PriceHistoryId
);

public interface IPriceResolutionService
{
    /// <summary>
    /// Resolves the current selling price from ProductVariantPriceHistory only.
    /// 1. Variant-specific price (PartId + VariantId match)
    /// 2. Base product price (PartId match, VariantId = null)
    /// Returns null if no price has been set yet.
    /// </summary>
    Task<PriceResolutionResult?> ResolveAsync(
        Guid partId,
        Guid? productVariantId,
        CancellationToken cancellationToken = default);
}
