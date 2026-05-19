namespace AutoPartShop.Application.DTOs.CurrencyDtos;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request DTO for creating a new exchange rate
/// </summary>
public class CreateExchangeRateRequest
{
    [Required(ErrorMessage = "From currency ID is required")]
    public Guid FromCurrencyId { get; set; }

    [Required(ErrorMessage = "To currency ID is required")]
    public Guid ToCurrencyId { get; set; }

    [Required(ErrorMessage = "Exchange rate is required")]
    [Range(0.000001, double.MaxValue, ErrorMessage = "Exchange rate must be greater than zero")]
    public decimal Rate { get; set; }

    [Required(ErrorMessage = "Effective date is required")]
    public DateTime EffectiveDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    [StringLength(50, ErrorMessage = "Source cannot exceed 50 characters")]
    public string Source { get; set; } = "MANUAL";

    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
    public string Notes { get; set; } = string.Empty;
}
