using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Api.Services;

public interface IPricingValidationService
{
    decimal ValidateLinePricing(Product part, decimal unitPrice, decimal discountPercent);
    PricingCalculationResult CalculateLinePricingSnapshot(Product part, decimal unitPrice, decimal discountPercent);
}

public sealed record PricingCalculationResult(
    decimal EffectivePrice,
    decimal Mrp,
    bool IsValid,
    string? ValidationMessage);
