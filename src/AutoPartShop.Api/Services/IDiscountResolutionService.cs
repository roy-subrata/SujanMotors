using AutoPartShop.Application.DTOs.DiscountDtos;

namespace AutoPartShop.Api.Services;

public interface IDiscountResolutionService
{
    /// <summary>
    /// Resolves the best item-level discount for a part/variant (best of variant vs product wins).
    /// </summary>
    Task<DiscountResolutionResult> ResolveItemDiscountAsync(
        Guid partId,
        Guid? productVariantId,
        decimal unitPrice,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a cart-level discount (promo code or threshold). Stacks on top of item price.
    /// </summary>
    Task<DiscountResolutionResult> ResolveCartDiscountAsync(
        decimal cartSubtotal,
        string? promoCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch-resolves item discounts for multiple line items in one pass (avoids N+1 queries).
    /// Returns a list of results in the same order as the input items.
    /// </summary>
    Task<IList<DiscountResolutionResult>> ResolveItemDiscountsAsync(
        IList<(Guid PartId, Guid? ProductVariantId, decimal UnitPrice)> items,
        CancellationToken cancellationToken = default);
}
