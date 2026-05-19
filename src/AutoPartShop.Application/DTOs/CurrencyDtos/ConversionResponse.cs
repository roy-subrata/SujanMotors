namespace AutoPartShop.Application.DTOs.CurrencyDtos;

/// <summary>
/// Response DTO for currency conversion
/// </summary>
public class ConversionResponse
{
    public decimal OriginalAmount { get; set; }
    public string OriginalCurrency { get; set; } = string.Empty;
    public decimal ConvertedAmount { get; set; }
    public string ConvertedCurrency { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime ConversionTimestamp { get; set; }
}
