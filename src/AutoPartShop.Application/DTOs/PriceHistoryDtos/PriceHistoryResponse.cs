namespace AutoPartShop.Application.DTOs.PriceHistoryDtos;

public class PriceHistoryResponse
{
    public Guid Id { get; set; }
    public Guid PartId { get; set; }
    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }
    public decimal PriceDifference { get; set; }
    public decimal PercentageChange { get; set; }
    public DateTime EffectiveDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
