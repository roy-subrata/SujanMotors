namespace AutoPartShop.Application.DTOs.StockDtos;

public class CreateStockLevelRequest
{
    public Guid PartId { get; set; }
    public Guid? VariantId { get; set; }
    public Guid WarehouseId { get; set; }
    public int Quantity { get; set; }
    public int ReorderLevel { get; set; }
    public int ReorderQuantity { get; set; }
}
