using AutoPartShop.Application.Common;

namespace AutoPartShop.Application.Stock.Dtos;

public class StockLotQuery : BaseQuery
{
    public Guid? PartId { get; set; }
    public Guid? WarehouseId { get; set; }
}
