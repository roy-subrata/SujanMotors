namespace AutoPartShop.Application.DTOs.StockDtos;

public class UpdateStockLevelRequest
{
    public int Quantity { get; set; }
    public int ReorderLevel { get; set; }
    public int ReorderQuantity { get; set; }
}
