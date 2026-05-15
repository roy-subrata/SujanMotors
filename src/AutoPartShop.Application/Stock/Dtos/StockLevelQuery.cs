using AutoPartShop.Application.Common;

namespace AutoPartShop.Application.Stock.Dtos;

public class StockLevelQuery : BaseQuery
{
    public string? PartId { get; set; }
    public string? WarehouseId { get; set; }
    public string? Status { get; set; }
    public bool LowStockOnly { get; set; }
}
