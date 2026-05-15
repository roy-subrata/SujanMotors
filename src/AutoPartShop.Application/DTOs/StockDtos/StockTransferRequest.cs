namespace AutoPartShop.Application.DTOs.StockDtos;

public class StockTransferRequest
{
    public Guid PartId { get; set; }
    public Guid FromWarehouseId { get; set; }
    public Guid ToWarehouseId { get; set; }
    public int Quantity { get; set; }
    public int QuantityInBaseUnit { get; set; }
    public Guid? UnitId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
