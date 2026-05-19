namespace AutoPartShop.Application.DTOs.UnitDtos;

public class CreateUnitRequest
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
