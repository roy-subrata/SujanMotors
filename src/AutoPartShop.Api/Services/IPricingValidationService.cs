using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Api.Services;

public interface IPricingValidationService
{
    decimal ValidateLinePricing(Part part, decimal unitPrice, decimal discountPercent);
    PricingCalculationResult CalculateLinePricingSnapshot(Part part, decimal unitPrice, decimal discountPercent);
}

public sealed record PricingCalculationResult(
    decimal EffectivePrice,
    decimal MinAllowedPrice,
    decimal MaxDiscountedPrice,
    decimal MinMarginPercent,
    decimal MaxDiscountPercent,
    decimal Mrp,
    decimal FloorPrice,
    bool IsValid,
    string? ValidationMessage);
