namespace AutoPartShop.Application.DTOs.WarrantyDtos;

public class WarrantyClaimResponse
{
    public Guid Id { get; set; }
    public string ClaimNumber { get; set; } = string.Empty;
    public Guid WarrantyRegistrationId { get; set; }
    public string WarrantyNumber { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public string PartSKU { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public Guid? TechnicianId { get; set; }
    public string? TechnicianName { get; set; }
    public DateTime ClaimDate { get; set; }
    public string IssueDescription { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public DateTime? RejectedDate { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ServiceStartDate { get; set; }
    public DateTime? ServiceCompletedDate { get; set; }
    public decimal ServiceCost { get; set; }
    public string ServiceCostCurrency { get; set; } = "BDT";
    public string? ServiceNotes { get; set; }
    public string? ResolutionDetails { get; set; }
    public bool IsOpen { get; set; }
    public bool CanBeModified { get; set; }
    public int DaysOpen { get; set; }
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime ModifiedDate { get; set; }
    public string ModifiedBy { get; set; } = string.Empty;
}
