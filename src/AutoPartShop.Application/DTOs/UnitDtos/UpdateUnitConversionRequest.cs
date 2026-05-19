namespace AutoPartShop.Application.DTOs.UnitDtos;

public class UpdateUnitConversionRequest
{
    public Guid Id { get; set; }
    public decimal ConversionFactor { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
