namespace AutoPartShop.Application.DTOs.StockDtos;

public class StockAdjustmentRequest
{
    public Guid PartId { get; set; }
    public Guid? VariantId { get; set; }
    public Guid WarehouseId { get; set; }
    public int Quantity { get; set; } // Positive for increase, negative for decrease
    public int QuantityInBaseUnit { get; set; }
    public Guid? UnitId { get; set; }
    public string Reason { get; set; } = string.Empty; // DAMAGED, EXPIRED, LOST, FOUND, COUNT_CORRECTION, etc.
    public string Reference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
