namespace AutoPartShop.Application.DTOs.WarrantyDtos;

public class WarrantyRegistrationResponse
{
    public Guid Id { get; set; }
    public string WarrantyNumber { get; set; } = string.Empty;
    public Guid PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string PartSKU { get; set; } = string.Empty;
    public Guid? ProductVariantId { get; set; }
    public string? VariantName { get; set; }
    public string? VariantSku { get; set; }
    public Guid SalesOrderId { get; set; }
    public string SalesOrderNumber { get; set; } = string.Empty;
    public Guid SalesOrderLineId { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public DateTime WarrantyStartDate { get; set; }
    public DateTime WarrantyExpiryDate { get; set; }
    public string WarrantyType { get; set; } = string.Empty;
    public int WarrantyPeriodMonths { get; set; }
    public string WarrantyTerms { get; set; } = string.Empty;
    public string GuaranteeMessage { get; set; } = string.Empty;
    public string CertificateNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? VoidReason { get; set; }
    public DateTime? VoidedDate { get; set; }
    public bool IsValid { get; set; }
    public int DaysUntilExpiry { get; set; }
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime ModifiedDate { get; set; }
    public string ModifiedBy { get; set; } = string.Empty;
}
