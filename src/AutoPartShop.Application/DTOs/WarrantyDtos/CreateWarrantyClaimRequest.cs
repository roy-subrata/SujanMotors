namespace AutoPartShop.Application.DTOs.WarrantyDtos;

public class CreateWarrantyClaimRequest
{
    public Guid WarrantyRegistrationId { get; set; }
    public Guid CustomerId { get; set; }
    public DateTime ClaimDate { get; set; }
    public string IssueDescription { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;  // REPAIR, REPLACEMENT, REFUND
    public string ServiceCostCurrency { get; set; } = "BDT";
}
