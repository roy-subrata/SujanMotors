namespace AutoPartShop.Api.Services;

public sealed class PricingRulesOptions
{
    public decimal MinMarginPercent { get; set; } = 15;
    public decimal MaxDiscountPercent { get; set; } = 20;
}
