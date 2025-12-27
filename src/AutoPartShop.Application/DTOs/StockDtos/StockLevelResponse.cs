namespace AutoPartShop.Application.DTOs.StockDtos;

public class StockLevelResponse
{
    public Guid Id { get; set; }
    public Guid PartId { get; set; }
    public Guid WarehouseId { get; set; }
    public int Quantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public int ReorderLevel { get; set; }
    public int ReorderQuantity { get; set; }
    public bool NeedsReorder { get; set; }
    public DateTime CreatedAt { get; set; }
}
