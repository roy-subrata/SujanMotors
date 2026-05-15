namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Represents a warranty claim for after-sale service
/// </summary>
public class WarrantyClaim : AuditableEntity
{
    public string ClaimNumber { get; private set; } = string.Empty;  // Auto-generated: WC-YYYY-XXXXX
    public Guid WarrantyRegistrationId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid? TechnicianId { get; private set; }
    public DateTime ClaimDate { get; private set; }
    public string IssueDescription { get; private set; } = string.Empty;
    public string ServiceType { get; private set; } = string.Empty;  // REPAIR, REPLACEMENT, REFUND
    public string Status { get; private set; } = "PENDING";  // PENDING, UNDER_REVIEW, APPROVED, REJECTED, IN_PROGRESS, COMPLETED, CLOSED
    public string? RejectionReason { get; private set; }
    public DateTime? RejectedDate { get; private set; }
    public DateTime? ApprovedDate { get; private set; }
    public string? ApprovedBy { get; private set; }
    public DateTime? ServiceStartDate { get; private set; }
    public DateTime? ServiceCompletedDate { get; private set; }
    public decimal ServiceCost { get; private set; } = 0;  // Cost even if customer doesn't pay, for analytics
    public string ServiceCostCurrency { get; private set; } = "BDT";
    public string? ServiceNotes { get; private set; }
    public string? ResolutionDetails { get; private set; }

    // Navigation properties
    public WarrantyRegistration? WarrantyRegistration { get; set; }
    public Customer? Customer { get; set; }
    public Technician? Technician { get; set; }

    private WarrantyClaim() { }

    public static WarrantyClaim Create(
        string claimNumber,
        Guid warrantyRegistrationId,
        Guid customerId,
        DateTime claimDate,
        string issueDescription,
        string serviceType,
        string serviceCostCurrency = "BDT")
    {
        if (string.IsNullOrWhiteSpace(claimNumber))
            throw new ArgumentException("Claim number cannot be empty", nameof(claimNumber));

        if (warrantyRegistrationId == Guid.Empty)
            throw new ArgumentException("Warranty registration ID cannot be empty", nameof(warrantyRegistrationId));

        if (customerId == Guid.Empty)
            throw new ArgumentException("Customer ID cannot be empty", nameof(customerId));

        if (string.IsNullOrWhiteSpace(issueDescription))
            throw new ArgumentException("Issue description cannot be empty", nameof(issueDescription));

        if (string.IsNullOrWhiteSpace(serviceType))
            throw new ArgumentException("Service type cannot be empty", nameof(serviceType));

        var validServiceTypes = new[] { "REPAIR", "REPLACEMENT", "REFUND" };
        if (!validServiceTypes.Contains(serviceType.ToUpper()))
            throw new ArgumentException($"Service type must be one of: {string.Join(", ", validServiceTypes)}", nameof(serviceType));

        return new WarrantyClaim
        {
            ClaimNumber = claimNumber.Trim().ToUpper(),
            WarrantyRegistrationId = warrantyRegistrationId,
            CustomerId = customerId,
            ClaimDate = claimDate,
            IssueDescription = issueDescription.Trim(),
            ServiceType = serviceType.Trim().ToUpper(),
            ServiceCostCurrency = serviceCostCurrency.Trim().ToUpper(),
            Status = "PENDING",
            CreatedBy = "System",
            ModifiedBy = "System"
        };
    }

    public void SubmitForReview()
    {
        if (Status != "PENDING")
            throw new InvalidOperationException($"Cannot submit for review. Current status: {Status}");

        Status = "UNDER_REVIEW";
        ModifiedBy = "System";
    }

    public void Approve(string approvedBy)
    {
        if (Status != "UNDER_REVIEW")
            throw new InvalidOperationException($"Cannot approve. Current status: {Status}");

        if (string.IsNullOrWhiteSpace(approvedBy))
            throw new ArgumentException("Approver name is required", nameof(approvedBy));

        Status = "APPROVED";
        ApprovedDate = DateTime.UtcNow;
        ApprovedBy = approvedBy.Trim();
        ModifiedBy = "System";
    }

    public void Reject(string rejectionReason, string rejectedBy)
    {
        if (Status == "REJECTED")
            throw new InvalidOperationException("Claim is already rejected");

        if (Status == "IN_PROGRESS" || Status == "COMPLETED" || Status == "CLOSED")
            throw new InvalidOperationException($"Cannot reject. Current status: {Status}");

        if (string.IsNullOrWhiteSpace(rejectionReason))
            throw new ArgumentException("Rejection reason is required", nameof(rejectionReason));

        Status = "REJECTED";
        RejectionReason = rejectionReason.Trim();
        RejectedDate = DateTime.UtcNow;
        ModifiedBy = rejectedBy?.Trim() ?? "System";
    }

    public void AssignTechnician(Guid technicianId)
    {
        if (Status != "APPROVED")
            throw new InvalidOperationException($"Cannot assign technician. Claim must be approved first. Current status: {Status}");

        if (technicianId == Guid.Empty)
            throw new ArgumentException("Technician ID cannot be empty", nameof(technicianId));

        TechnicianId = technicianId;
        Status = "IN_PROGRESS";
        ServiceStartDate = DateTime.UtcNow;
        ModifiedBy = "System";
    }

    public void StartServiceWithoutTechnician()
    {
        if (Status != "APPROVED")
            throw new InvalidOperationException($"Cannot start service. Claim must be approved first. Current status: {Status}");

        if (ServiceType != "REPLACEMENT" && ServiceType != "REFUND")
            throw new InvalidOperationException($"Service without technician is only allowed for replacement/refund claims. Current service type: {ServiceType}");

        Status = "IN_PROGRESS";
        ServiceStartDate = DateTime.UtcNow;
        ModifiedBy = "System";
    }

    public void UpdateServiceCost(decimal serviceCost, string? serviceNotes = null)
    {
        if (Status == "CLOSED" || Status == "REJECTED")
            throw new InvalidOperationException($"Cannot update service cost on a {Status} claim");

        if (serviceCost < 0)
            throw new ArgumentException("Service cost cannot be negative", nameof(serviceCost));

        ServiceCost = serviceCost;
        if (!string.IsNullOrWhiteSpace(serviceNotes))
        {
            ServiceNotes = serviceNotes.Trim();
        }
        ModifiedBy = "System";
    }

    public void Complete(string resolutionDetails)
    {
        if (Status != "IN_PROGRESS")
            throw new InvalidOperationException($"Cannot complete. Current status: {Status}");

        if (string.IsNullOrWhiteSpace(resolutionDetails))
            throw new ArgumentException("Resolution details are required", nameof(resolutionDetails));

        Status = "COMPLETED";
        ServiceCompletedDate = DateTime.UtcNow;
        ResolutionDetails = resolutionDetails.Trim();
        ModifiedBy = "System";
    }

    public void Close(string? notes = null)
    {
        if (Status != "COMPLETED" && Status != "REJECTED")
            throw new InvalidOperationException($"Cannot close. Claim must be completed or rejected first. Current status: {Status}");

        Status = "CLOSED";
        if (!string.IsNullOrWhiteSpace(notes))
        {
            ServiceNotes = string.IsNullOrWhiteSpace(ServiceNotes)
                ? notes.Trim()
                : $"{ServiceNotes} | {notes.Trim()}";
        }
        ModifiedBy = "System";
    }

    public bool IsOpen()
    {
        return Status != "CLOSED" && Status != "REJECTED";
    }

    public bool CanBeModified()
    {
        return Status == "PENDING" || Status == "UNDER_REVIEW";
    }
}
