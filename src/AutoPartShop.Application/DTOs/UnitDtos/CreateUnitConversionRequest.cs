namespace AutoPartShop.Application.DTOs.UnitDtos;

public class CreateUnitConversionRequest
{
    public Guid FromUnitId { get; set; }
    public Guid ToUnitId { get; set; }
    public decimal ConversionFactor { get; set; }
    public string Description { get; set; } = string.Empty;
}
