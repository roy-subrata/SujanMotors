using AutoPartShop.Application.Common;

namespace AutoPartShop.Application.Stock.Dtos;

public class StockMovementQuery : BaseQuery
{
    public Guid? PartId { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? Type { get; set; }
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
