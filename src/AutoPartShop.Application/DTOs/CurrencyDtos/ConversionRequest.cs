namespace AutoPartShop.Application.DTOs.CurrencyDtos;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request DTO for currency conversion
/// </summary>
public class ConversionRequest
{
    [Required(ErrorMessage = "Amount is required")]
    // Zero is a legitimate amount to convert (a zero-value line still has a currency), so only
    // negatives are refused.
    [Range(0, double.MaxValue, ErrorMessage = "Amount cannot be negative")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "From currency code is required")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency code must be exactly 3 characters")]
    public string FromCurrency { get; set; } = string.Empty;

    [Required(ErrorMessage = "To currency code is required")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency code must be exactly 3 characters")]
    public string ToCurrency { get; set; } = string.Empty;

    public DateTime? EffectiveDate { get; set; }
}
