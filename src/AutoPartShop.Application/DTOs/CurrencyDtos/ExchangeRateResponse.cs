namespace AutoPartShop.Application.DTOs.CurrencyDtos;

/// <summary>
/// Response DTO for ExchangeRate
/// </summary>
public class ExchangeRateResponse
{
    public Guid Id { get; set; }
    public Guid FromCurrencyId { get; set; }
    public string FromCurrencyCode { get; set; } = string.Empty;
    public string FromCurrencyName { get; set; } = string.Empty;
    public Guid ToCurrencyId { get; set; }
    public string ToCurrencyCode { get; set; } = string.Empty;
    public string ToCurrencyName { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string Source { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
