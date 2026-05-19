namespace AutoPartShop.Application.DTOs.CurrencyDtos;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request DTO for creating a new currency
/// </summary>
public class CreateCurrencyRequest
{
    [Required(ErrorMessage = "Currency code is required")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency code must be exactly 3 characters (ISO 4217)")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Currency name is required")]
    [StringLength(100, ErrorMessage = "Currency name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Currency symbol is required")]
    [StringLength(10, ErrorMessage = "Currency symbol cannot exceed 10 characters")]
    public string Symbol { get; set; } = string.Empty;

    [Range(0, 4, ErrorMessage = "Decimal places must be between 0 and 4")]
    public int DecimalPlaces { get; set; } = 2;

    public bool IsActive { get; set; } = true;

    public bool IsBaseCurrency { get; set; } = false;

    [Range(0, int.MaxValue, ErrorMessage = "Display order must be non-negative")]
    public int DisplayOrder { get; set; } = 0;
}
