namespace AutoPartShop.Application.DTOs.StockDtos;

public class CreateStockLevelRequest
{
    public Guid PartId { get; set; }
    public Guid? VariantId { get; set; }
    public Guid WarehouseId { get; set; }
    /// <summary>
    /// Must be 0. A stock level is opened empty; stock enters only through an audited adjustment
    /// or goods receipt. Retained so an older client sending it gets a clear 400 rather than
    /// having the value silently dropped.
    /// </summary>
    public int Quantity { get; set; }
    public int ReorderLevel { get; set; }
    public int ReorderQuantity { get; set; }
}
