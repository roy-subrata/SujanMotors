namespace AutoPartShop.Application.DTOs.PriceHistoryDtos;

public class CreatePriceHistoryRequest
{
    public Guid PartId { get; set; }
    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ChangedBy { get; set; } = string.Empty;
}
