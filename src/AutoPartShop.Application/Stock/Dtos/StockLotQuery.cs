using AutoPartShop.Application.Common;

namespace AutoPartShop.Application.Stock.Dtos;

public class StockLotQuery : BaseQuery
{
    public string PartId { get; set; } = string.Empty;
    public string WarehouseId { get; set; } = string.Empty;
    public string? VariantId { get; set; }
}
