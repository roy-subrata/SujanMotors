using AutoPartShop.Application.Common;

namespace AutoPartShop.Application.Stock.Dtos;

public class StockLevelQuery : BaseQuery
{
    public Guid? PartId { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? Status { get; set; }
    public bool LowStockOnly { get; set; }
}
