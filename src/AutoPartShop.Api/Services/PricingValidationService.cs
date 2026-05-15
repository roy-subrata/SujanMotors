using AutoPartShop.Domain.Entities;
using Microsoft.Extensions.Options;

namespace AutoPartShop.Api.Services;

public sealed class PricingValidationService : IPricingValidationService
{
    public decimal ValidateLinePricing(Part part, decimal unitPrice, decimal discountPercent)
    {
        if (part is null) throw new ArgumentException("Part is required for pricing validation.");
        if (unitPrice <= 0) throw new ArgumentException("Selling price must be greater than 0.");
        if (discountPercent < 0 || discountPercent > 100)
            throw new ArgumentException("Discount percentage must be between 0 and 100.");

        var effectivePrice = unitPrice - (unitPrice * (discountPercent / 100));
        if (effectivePrice < 0) throw new ArgumentException("Selling price cannot be negative.");

        return effectivePrice;
    }

    public PricingCalculationResult CalculateLinePricingSnapshot(Part part, decimal unitPrice, decimal discountPercent)
    {
        if (part is null)
            return new PricingCalculationResult(0, 0, false, "Part is required.");

        var effectivePrice = unitPrice - (unitPrice * (discountPercent / 100));
        var mrp = part.SellingPrice;

        if (unitPrice <= 0)
            return new PricingCalculationResult(effectivePrice, mrp, false, "Selling price must be greater than 0.");

        if (discountPercent < 0 || discountPercent > 100)
            return new PricingCalculationResult(effectivePrice, mrp, false, "Discount must be between 0 and 100.");

        return new PricingCalculationResult(effectivePrice, mrp, true, null);
    }
}
