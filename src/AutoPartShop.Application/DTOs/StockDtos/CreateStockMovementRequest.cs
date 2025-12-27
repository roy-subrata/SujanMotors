namespace AutoPartShop.Application.DTOs.StockDtos;

public class CreateStockMovementRequest
{
    public Guid PartId { get; set; }
    public Guid WarehouseId { get; set; }
    public string Type { get; set; } = "IN"; // IN, OUT, RETURN, ADJUST, TRANSFER
    public int Quantity { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
