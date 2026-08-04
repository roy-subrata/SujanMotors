namespace AutoPartShop.Application.Stock.Dtos;

public class StockLotFilterSummaryResponse
{
    public decimal TotalCost { get; set; }
    public decimal AvailableCost { get; set; }
    public decimal AverageCostPerUnit { get; set; }
    public int TotalQuantityAvailableInBaseUnit { get; set; }
}
