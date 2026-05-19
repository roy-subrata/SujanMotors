namespace AutoPartShop.Application.DTOs.StockDtos;

public class StockLevelResponse
{
    public Guid Id { get; set; }
    public Guid PartId { get; set; }
    public Guid WarehouseId { get; set; }
    public int Quantity { get; set; }
    public int QuantityInBaseUnit { get; set; }
    public int ReservedQuantity { get; set; }
    public int ReservedQuantityInBaseUnit { get; set; }
    public int AvailableQuantity { get; set; }
    public int AvailableQuantityInBaseUnit { get; set; }
    public int ReorderLevel { get; set; }
    public int ReorderQuantity { get; set; }
    public bool NeedsReorder { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitName { get; set; }
    public string? UnitSymbol { get; set; }
    public string? BaseUnitName { get; set; }
    public string? BaseUnitSymbol { get; set; }
    public DateTime CreatedAt { get; set; }
}
