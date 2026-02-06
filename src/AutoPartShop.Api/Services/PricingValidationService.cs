using AutoPartShop.Domain.Entities;
using Microsoft.Extensions.Options;

namespace AutoPartShop.Api.Services;

public sealed class PricingValidationService : IPricingValidationService
{
    private readonly PricingRulesOptions _options;

    public PricingValidationService(IOptions<PricingRulesOptions> options)
    {
        _options = options.Value ?? new PricingRulesOptions();
    }

    public decimal ValidateLinePricing(Part part, decimal unitPrice, decimal discountPercent)
    {
        if (part is null)
            throw new ArgumentException("Part is required for pricing validation.");

        if (unitPrice <= 0)
            throw new ArgumentException("Selling price must be greater than 0.");

        if (discountPercent < 0 || discountPercent > 100)
            throw new ArgumentException("Discount percentage must be between 0 and 100.");

        var mrp = part.SellingPrice;
        if (mrp <= 0)
            throw new ArgumentException("MRP must be configured for this item before it can be sold.");

        var effectivePrice = unitPrice - (unitPrice * (discountPercent / 100));
        if (effectivePrice < 0)
            throw new ArgumentException("Selling price cannot be negative.");

        if (effectivePrice > mrp)
            throw new ArgumentException("Selling price cannot exceed MRP.");

        var minMarginPercent = Math.Max(0, part.MinMarginPercentOverride ?? _options.MinMarginPercent);
        var maxDiscountPercent = Math.Max(0, part.MaxDiscountPercentOverride ?? _options.MaxDiscountPercent);

        var minAllowedPrice = part.CostPrice + (part.CostPrice * (minMarginPercent / 100));
        var maxDiscountedPrice = mrp - (mrp * (maxDiscountPercent / 100));
        var floorPrice = Math.Max(minAllowedPrice, maxDiscountedPrice);

        if (effectivePrice < floorPrice)
            throw new ArgumentException("Selling price is below the minimum allowed for this item.");

        return effectivePrice;
    }

    public PricingCalculationResult CalculateLinePricingSnapshot(Part part, decimal unitPrice, decimal discountPercent)
    {
        if (part is null)
        {
            return new PricingCalculationResult(0, 0, 0, 0, 0, 0, 0, false, "Part is required for pricing validation.");
        }

        var mrp = part.SellingPrice;
        var minMarginPercent = Math.Max(0, part.MinMarginPercentOverride ?? _options.MinMarginPercent);
        var maxDiscountPercent = Math.Max(0, part.MaxDiscountPercentOverride ?? _options.MaxDiscountPercent);

        var minAllowedPrice = part.CostPrice + (part.CostPrice * (minMarginPercent / 100));
        var maxDiscountedPrice = mrp - (mrp * (maxDiscountPercent / 100));
        var floorPrice = Math.Max(minAllowedPrice, maxDiscountedPrice);

        var effectivePrice = unitPrice - (unitPrice * (discountPercent / 100));
        var isValid = true;
        string? message = null;

        if (unitPrice <= 0)
        {
            isValid = false;
            message = "Selling price must be greater than 0.";
        }
        else if (discountPercent < 0 || discountPercent > 100)
        {
            isValid = false;
            message = "Discount percentage must be between 0 and 100.";
        }
        else if (mrp <= 0)
        {
            isValid = false;
            message = "MRP must be configured for this item before it can be sold.";
        }
        else if (effectivePrice < 0)
        {
            isValid = false;
            message = "Selling price cannot be negative.";
        }
        else if (effectivePrice > mrp)
        {
            isValid = false;
            message = "Selling price cannot exceed MRP.";
        }
        else if (effectivePrice < floorPrice)
        {
            isValid = false;
            message = "Selling price is below the minimum allowed for this item.";
        }

        return new PricingCalculationResult(
            effectivePrice,
            minAllowedPrice,
            maxDiscountedPrice,
            minMarginPercent,
            maxDiscountPercent,
            mrp,
            floorPrice,
            isValid,
            message);
    }
}
