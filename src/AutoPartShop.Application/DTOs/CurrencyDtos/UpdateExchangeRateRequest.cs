namespace AutoPartShop.Application.DTOs.CurrencyDtos;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request DTO for updating an existing exchange rate
/// </summary>
public class UpdateExchangeRateRequest
{
    [Required(ErrorMessage = "Exchange rate is required")]
    [Range(0.000001, double.MaxValue, ErrorMessage = "Exchange rate must be greater than zero")]
    public decimal Rate { get; set; }

    [Required(ErrorMessage = "Effective date is required")]
    public DateTime EffectiveDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    [StringLength(50, ErrorMessage = "Source cannot exceed 50 characters")]
    public string Source { get; set; } = "MANUAL";

    public bool IsActive { get; set; } = true;

    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
    public string Notes { get; set; } = string.Empty;
}
