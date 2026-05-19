namespace AutoPartShop.Application.DTOs.CurrencyDtos;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request DTO for updating an existing currency
/// </summary>
public class UpdateCurrencyRequest
{
    [Required(ErrorMessage = "Currency name is required")]
    [StringLength(100, ErrorMessage = "Currency name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Currency symbol is required")]
    [StringLength(10, ErrorMessage = "Currency symbol cannot exceed 10 characters")]
    public string Symbol { get; set; } = string.Empty;

    [Range(0, 4, ErrorMessage = "Decimal places must be between 0 and 4")]
    public int DecimalPlaces { get; set; } = 2;

    public bool IsActive { get; set; } = true;

    [Range(0, int.MaxValue, ErrorMessage = "Display order must be non-negative")]
    public int DisplayOrder { get; set; } = 0;
}
