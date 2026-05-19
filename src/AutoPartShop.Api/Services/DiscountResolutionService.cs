using AutoPartShop.Application.DTOs.DiscountDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;

namespace AutoPartShop.Api.Services;

public class DiscountResolutionService : IDiscountResolutionService
{
    private readonly IDiscountRepository _discountRepository;

    public DiscountResolutionService(IDiscountRepository discountRepository)
    {
        _discountRepository = discountRepository;
    }

    public async Task<DiscountResolutionResult> ResolveItemDiscountAsync(
        Guid partId,
        Guid? productVariantId,
        decimal unitPrice,
        CancellationToken cancellationToken = default)
    {
        Discount? variantDiscount = null;
        Discount? productDiscount = null;

        if (productVariantId.HasValue)
            variantDiscount = await _discountRepository.GetVariantDiscountAsync(partId, productVariantId.Value, cancellationToken);

        productDiscount = await _discountRepository.GetProductDiscountAsync(partId, cancellationToken);

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
        Discount? cartDiscount = null;

        // 1. Promo code
        if (!string.IsNullOrWhiteSpace(promoCode))
        {
            var byCode = await _discountRepository.GetByPromoCodeAsync(promoCode, cancellationToken);
            if (byCode != null && byCode.IsValidOn(DateTime.UtcNow) && byCode.IsCartLevel)
                cartDiscount = byCode;
        }

        // 2. Threshold discount (best matching one)
        if (cartDiscount == null)
        {
            var active = await _discountRepository.GetActiveDiscountsAsync(cancellationToken);
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
            FinalPrice = price
        };
}
