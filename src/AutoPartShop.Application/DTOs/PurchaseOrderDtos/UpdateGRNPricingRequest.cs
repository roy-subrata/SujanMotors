namespace AutoPartShop.Application.DTOs.PurchaseOrderDtos;

public class UpdateGRNPricingRequest
{
    public List<UpdateGRNLinePricingRequest> Lines { get; set; } = new();
}

public class UpdateGRNLinePricingRequest
{
    public Guid LineId { get; set; }
    public bool? HasWarranty { get; set; }
    public int? WarrantyPeriodMonths { get; set; }
    public string? WarrantyType { get; set; }
    public string? WarrantyTerms { get; set; }
}
