namespace AutoPartShop.Application.DTOs.StockDtos;

public class StockAdjustmentResponse
{
    public Guid Id { get; set; }
    public Guid PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string PartCode { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int QuantityInBaseUnit { get; set; }
    public Guid? UnitId { get; set; }
    public int PreviousQuantity { get; set; }
    public int PreviousQuantityInBaseUnit { get; set; }
    public int NewQuantity { get; set; }
    public int NewQuantityInBaseUnit { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
