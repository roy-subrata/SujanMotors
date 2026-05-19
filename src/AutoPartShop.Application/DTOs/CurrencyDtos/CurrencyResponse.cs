namespace AutoPartShop.Application.DTOs.CurrencyDtos;

/// <summary>
/// Response DTO for Currency
/// </summary>
public class CurrencyResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public int DecimalPlaces { get; set; }
    public bool IsActive { get; set; }
    public bool IsBaseCurrency { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
