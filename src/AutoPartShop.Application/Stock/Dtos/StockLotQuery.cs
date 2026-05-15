using AutoPartShop.Application.Common;

namespace AutoPartShop.Application.Stock.Dtos;

public class StockLotQuery : BaseQuery
{
    public string PartId { get; set; }
    public string WarehouseId { get; set; }
}
