using AutoPartShop.Application.DTOs.DiscountDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;

namespace AutoPartShop.Api.Services;

public class DiscountResolutionService : IDiscountResolutionService
{
    private readonly IDiscountRepository _discountRepository;
    private readonly IShopClock _shopClock;

    public DiscountResolutionService(IDiscountRepository discountRepository, IShopClock shopClock)
    {
        _discountRepository = discountRepository;
        _shopClock = shopClock;
    }

    public async Task<DiscountResolutionResult> ResolveItemDiscountAsync(
        Guid partId,
        Guid? productVariantId,
        decimal unitPrice,
        CancellationToken cancellationToken = default)
    {
        var today = _shopClock.Today.ToDateTime(TimeOnly.MinValue);

        var variantDiscount = productVariantId.HasValue
            ? (await _discountRepository.GetVariantDiscountsAsync(partId, productVariantId.Value, today, cancellationToken))
                .OrderByDescending(d => d.CalculateDiscountAmount(unitPrice))
                .FirstOrDefault()
            : null;

        var productDiscount = (await _discountRepository.GetProductDiscountsAsync(partId, today, cancellationToken))
            .OrderByDescending(d => d.CalculateDiscountAmount(unitPrice))
            .FirstOrDefault();

        var variantAmount = variantDiscount?.CalculateDiscountAmount(unitPrice) ?? 0;
        var productAmount = productDiscount?.CalculateDiscountAmount(unitPrice) ?? 0;

        if (variantAmount == 0 && productAmount == 0)
            return NoDiscount(unitPrice);

        return variantAmount >= productAmount
            ? BuildResult(variantDiscount!, variantAmount, unitPrice, "VARIANT")
            : BuildResult(productDiscount!, productAmount, unitPrice, "PRODUCT");
    }

    public async Task<DiscountResolutionResult> ResolveCartDiscountAsync(
        decimal cartSubtotal,
        string? promoCode,
        CancellationToken cancellationToken = default)
    {
        var today = _shopClock.Today.ToDateTime(TimeOnly.MinValue);

        Discount? cartDiscount = null;

        // 1. Promo code — if provided, only use promo code; do NOT fall through to threshold
        if (!string.IsNullOrWhiteSpace(promoCode))
        {
            var byCode = await _discountRepository.GetByPromoCodeAsync(promoCode, cancellationToken);
            if (byCode != null && byCode.IsValidOn(today) && byCode.IsCartLevel)
                cartDiscount = byCode;

            // If promo code was provided but not found/expired, return no discount
            // (do NOT fall through to threshold)
            if (cartDiscount == null)
                return NoDiscount(cartSubtotal);
        }

        // 2. Threshold discount (only when no promo code was provided)
        if (cartDiscount == null)
        {
            var active = await _discountRepository.GetActiveDiscountsAsync(today, cancellationToken);
            cartDiscount = active
                .Where(d =>
                    d.IsCartLevel &&
                    d.PromoCode == null &&
                    d.MinimumCartAmount.HasValue &&
                    cartSubtotal >= d.MinimumCartAmount.Value)
                .OrderByDescending(d => d.CalculateDiscountAmount(cartSubtotal))
                .FirstOrDefault();
        }

        if (cartDiscount == null)
            return NoDiscount(cartSubtotal);

        var amount = cartDiscount.CalculateDiscountAmount(cartSubtotal);
        return BuildResult(cartDiscount, amount, cartSubtotal, "CART");
    }

    private static DiscountResolutionResult BuildResult(Discount d, decimal amount, decimal price, string level) =>
        new()
        {
            DiscountId = d.Id,
            DiscountName = d.Name,
            DiscountType = d.Type,
            DiscountValue = d.Value,
            DiscountAmount = amount,
            AppliedLevel = level,
            FinalPrice = Math.Max(0, price - amount)
        };

    private static DiscountResolutionResult NoDiscount(decimal price) =>
        new()
        {
            DiscountId = null,
            DiscountName = null,
            DiscountType = null,
            DiscountValue = 0,
            DiscountAmount = 0,
            AppliedLevel = "NONE",
            FinalPrice = Math.Max(0, price)
        };

    public async Task<IList<DiscountResolutionResult>> ResolveItemDiscountsAsync(
        IList<(Guid PartId, Guid? ProductVariantId, decimal UnitPrice)> items,
        CancellationToken cancellationToken = default)
    {
        var today = _shopClock.Today.ToDateTime(TimeOnly.MinValue);
        var results = new List<DiscountResolutionResult>(items.Count);

        // Collect unique partIds so we can batch-load discounts
        var partIds = items.Select(i => i.PartId).Distinct().ToList();

        // Pre-load all active product-level and variant-level discounts for these parts
        var allDiscounts = new List<Discount>();
        foreach (var partId in partIds)
        {
            var productDiscounts = await _discountRepository.GetProductDiscountsAsync(partId, today, cancellationToken);
            allDiscounts.AddRange(productDiscounts);
        }

        // Pre-load variant-level discounts for items that have a variant
        var variantItems = items.Where(i => i.ProductVariantId.HasValue).ToList();
        foreach (var item in variantItems)
        {
            var variantDiscounts = await _discountRepository.GetVariantDiscountsAsync(
                item.PartId, item.ProductVariantId!.Value, today, cancellationToken);
            allDiscounts.AddRange(variantDiscounts);
        }

        foreach (var item in items)
        {
            var variantDiscount = item.ProductVariantId.HasValue
                ? allDiscounts
                    .Where(d => d.PartId == item.PartId && d.ProductVariantId == item.ProductVariantId)
                    .OrderByDescending(d => d.CalculateDiscountAmount(item.UnitPrice))
                    .FirstOrDefault()
                : null;

            var productDiscount = allDiscounts
                .Where(d => d.PartId == item.PartId && !d.ProductVariantId.HasValue)
                .OrderByDescending(d => d.CalculateDiscountAmount(item.UnitPrice))
                .FirstOrDefault();

            var variantAmount = variantDiscount?.CalculateDiscountAmount(item.UnitPrice) ?? 0;
            var productAmount = productDiscount?.CalculateDiscountAmount(item.UnitPrice) ?? 0;

            if (variantAmount == 0 && productAmount == 0)
            {
                results.Add(NoDiscount(item.UnitPrice));
            }
            else if (variantAmount >= productAmount)
            {
                results.Add(BuildResult(variantDiscount!, variantAmount, item.UnitPrice, "VARIANT"));
            }
            else
            {
                results.Add(BuildResult(productDiscount!, productAmount, item.UnitPrice, "PRODUCT"));
            }
        }

        return results;
    }
}
