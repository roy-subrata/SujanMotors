namespace AutoPartShop.Application.DTOs.UnitDtos;

public class UnitConversionResponse
{
    public Guid Id { get; set; }
    public Guid FromUnitId { get; set; }
    public Guid ToUnitId { get; set; }
    public string FromUnitName { get; set; } = string.Empty;
    public string FromUnitCode { get; set; } = string.Empty;
    public string ToUnitName { get; set; } = string.Empty;
    public string ToUnitCode { get; set; } = string.Empty;
    public decimal ConversionFactor { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string ModifiedBy { get; set; } = string.Empty;
}
