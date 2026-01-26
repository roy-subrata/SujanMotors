namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Represents a warranty registration for a part sold to a customer
/// </summary>
public class WarrantyRegistration : AuditableEntity
{
    public string WarrantyNumber { get; private set; } = string.Empty;  // Auto-generated: WR-YYYY-XXXXX
    public Guid PartId { get; private set; }
    public Guid SalesOrderId { get; private set; }
    public Guid SalesOrderLineId { get; private set; }
    public Guid CustomerId { get; private set; }
    public DateTime SaleDate { get; private set; }
    public DateTime WarrantyStartDate { get; private set; }
    public DateTime WarrantyExpiryDate { get; private set; }
    public string WarrantyType { get; private set; } = string.Empty;  // MANUFACTURER, SELLER, EXTENDED
    public int WarrantyPeriodMonths { get; private set; }
    public string WarrantyTerms { get; private set; } = string.Empty;
    public string CertificateNumber { get; private set; } = string.Empty;  // Generated from template
    public string Status { get; private set; } = "ACTIVE";  // ACTIVE, EXPIRED, CLAIMED, VOID
    public string? VoidReason { get; private set; }
    public DateTime? VoidedDate { get; private set; }

    // Navigation properties
    public Part? Part { get; set; }
    public SalesOrder? SalesOrder { get; set; }
    public SalesOrderLine? SalesOrderLine { get; set; }
    public Customer? Customer { get; set; }
    public ICollection<WarrantyClaim> Claims { get; set; } = new List<WarrantyClaim>();

    private WarrantyRegistration() { }

    public static WarrantyRegistration Create(
        string warrantyNumber,
        Guid partId,
        Guid salesOrderId,
        Guid salesOrderLineId,
        Guid customerId,
        DateTime saleDate,
        DateTime warrantyStartDate,
        int warrantyPeriodMonths,
        string warrantyType,
        string warrantyTerms,
        string certificateNumber)
    {
        if (string.IsNullOrWhiteSpace(warrantyNumber))
            throw new ArgumentException("Warranty number cannot be empty", nameof(warrantyNumber));

        if (partId == Guid.Empty)
            throw new ArgumentException("Part ID cannot be empty", nameof(partId));

        if (salesOrderId == Guid.Empty)
            throw new ArgumentException("Sales order ID cannot be empty", nameof(salesOrderId));

        if (salesOrderLineId == Guid.Empty)
            throw new ArgumentException("Sales order line ID cannot be empty", nameof(salesOrderLineId));

        if (customerId == Guid.Empty)
            throw new ArgumentException("Customer ID cannot be empty", nameof(customerId));

        if (warrantyPeriodMonths <= 0)
            throw new ArgumentException("Warranty period must be greater than 0", nameof(warrantyPeriodMonths));

        if (string.IsNullOrWhiteSpace(warrantyType))
            throw new ArgumentException("Warranty type cannot be empty", nameof(warrantyType));

        if (string.IsNullOrWhiteSpace(certificateNumber))
            throw new ArgumentException("Certificate number cannot be empty", nameof(certificateNumber));

        var warrantyExpiryDate = warrantyStartDate.AddMonths(warrantyPeriodMonths);

        return new WarrantyRegistration
        {
            WarrantyNumber = warrantyNumber.Trim().ToUpper(),
            PartId = partId,
            SalesOrderId = salesOrderId,
            SalesOrderLineId = salesOrderLineId,
            CustomerId = customerId,
            SaleDate = saleDate,
            WarrantyStartDate = warrantyStartDate,
            WarrantyExpiryDate = warrantyExpiryDate,
            WarrantyPeriodMonths = warrantyPeriodMonths,
            WarrantyType = warrantyType.Trim().ToUpper(),
            WarrantyTerms = warrantyTerms?.Trim() ?? string.Empty,
            CertificateNumber = certificateNumber.Trim().ToUpper(),
            Status = "ACTIVE",
            CreatedBy = "System",
            ModifiedBy = "System"
        };
    }

    public void Void(string reason)
    {
        if (Status == "VOID")
            throw new InvalidOperationException("Warranty is already voided");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Void reason is required", nameof(reason));

        Status = "VOID";
        VoidReason = reason.Trim();
        VoidedDate = DateTime.UtcNow;
        ModifiedBy = "System";
    }

    public void MarkAsClaimed()
    {
        if (Status == "VOID")
            throw new InvalidOperationException("Cannot mark voided warranty as claimed");

        if (Status == "EXPIRED")
            throw new InvalidOperationException("Cannot mark expired warranty as claimed");

        Status = "CLAIMED";
        ModifiedBy = "System";
    }

    public void CheckAndUpdateExpiry()
    {
        if (Status == "ACTIVE" && DateTime.UtcNow > WarrantyExpiryDate)
        {
            Status = "EXPIRED";
            ModifiedBy = "System";
        }
    }

    public bool IsValid()
    {
        return Status == "ACTIVE" && DateTime.UtcNow <= WarrantyExpiryDate;
    }
}
