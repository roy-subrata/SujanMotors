namespace AutoPartShop.Application.DTOs.PurchaseOrderDtos;

public class UpdateGRNPricingRequest
{
    public List<UpdateGRNLinePricingRequest> Lines { get; set; } = new();
}

public class UpdateGRNLinePricingRequest
{
    public Guid LineId { get; set; }
    /// <summary>Per-received-unit cost. When provided (> 0), corrects the line's cost before accept.</summary>
    public decimal? UnitCost { get; set; }
    public bool? HasWarranty { get; set; }
    public int? WarrantyPeriodMonths { get; set; }
    public string? WarrantyType { get; set; }
    public string? WarrantyTerms { get; set; }
}
