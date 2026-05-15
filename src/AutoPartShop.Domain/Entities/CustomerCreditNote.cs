namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Represents a credit note issued to a customer for returned goods.
/// Credit notes can be applied as credit against future sales invoices from the same customer.
/// </summary>
public class CustomerCreditNote : AuditableEntity
{
    public string CreditNoteNumber { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public Guid? WarrantyClaimId { get; private set; }  // Optional: link to warranty refund claim
    public Guid? SalesReturnId { get; private set; }  // Link to the original return
    public Guid? InvoiceId { get; private set; }  // Invoice this credit was applied to (if any)
    public Guid? SalesOrderId { get; private set; }  // SO this credit was applied to (if any)
    public decimal TotalAmount { get; private set; }  // Original credit note amount
    public decimal UsedAmount { get; private set; } = 0;  // Amount already applied to invoices
    public decimal AvailableAmount => TotalAmount - UsedAmount;  // Remaining credit available
    public string Currency { get; private set; } = "USD";
    public DateTime IssueDate { get; private set; }
    public DateTime? ExpiryDate { get; private set; }  // Optional expiry date
    public string Status { get; private set; } = "AVAILABLE";  // AVAILABLE, PARTIALLY_USED, FULLY_USED, EXPIRED, CANCELLED
    public string Notes { get; private set; } = string.Empty;
    public string IssuedBy { get; private set; } = string.Empty;

    // Navigation properties
    public Customer? Customer { get; set; }
    public SalesReturn? SalesReturn { get; set; }
    public Invoice? Invoice { get; set; }
    public SalesOrder? SalesOrder { get; set; }

    private CustomerCreditNote() { }

    /// <summary>
    /// Create a new credit note from a sales return
    /// </summary>
    public static CustomerCreditNote Create(
        string creditNoteNumber,
        Guid customerId,
        Guid? salesReturnId,
        decimal amount,
        string currency = "USD",
        DateTime? issueDate = null,
        DateTime? expiryDate = null,
        string notes = "",
        string issuedBy = "")
    {
        if (string.IsNullOrWhiteSpace(creditNoteNumber))
            throw new ArgumentException("Credit note number cannot be empty", nameof(creditNoteNumber));

        if (customerId == Guid.Empty)
            throw new ArgumentException("CustomerId cannot be empty", nameof(customerId));

        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than 0", nameof(amount));

        if (expiryDate.HasValue && issueDate.HasValue && expiryDate.Value < issueDate.Value)
            throw new ArgumentException("Expiry date cannot be before issue date", nameof(expiryDate));

        return new CustomerCreditNote
        {
            CreditNoteNumber = creditNoteNumber.Trim().ToUpper(),
            CustomerId = customerId,
            SalesReturnId = salesReturnId,
            TotalAmount = amount,
            Currency = currency.Trim().ToUpper(),
            IssueDate = issueDate ?? DateTime.UtcNow,
            ExpiryDate = expiryDate,
            Status = "AVAILABLE",
            Notes = notes?.Trim() ?? string.Empty,
            IssuedBy = issuedBy?.Trim() ?? string.Empty
        };
    }

    /// <summary>
    /// Apply credit note amount to an invoice
    /// </summary>
    public void ApplyToInvoice(Guid invoiceId, Guid salesOrderId, decimal amountToApply)
    {
        if (Status == "CANCELLED")
            throw new InvalidOperationException("Cannot apply a cancelled credit note");

        if (Status == "EXPIRED")
            throw new InvalidOperationException("Cannot apply an expired credit note");

        if (amountToApply <= 0)
            throw new ArgumentException("Amount to apply must be greater than 0", nameof(amountToApply));

        if (amountToApply > AvailableAmount)
            throw new InvalidOperationException(
                $"Cannot apply {amountToApply}. Only {AvailableAmount} available on credit note {CreditNoteNumber}");

        UsedAmount += amountToApply;
        InvoiceId = invoiceId;
        SalesOrderId = salesOrderId;

        // Update status based on usage
        if (AvailableAmount <= 0)
            Status = "FULLY_USED";
        else if (UsedAmount > 0)
            Status = "PARTIALLY_USED";
    }

    public void LinkToWarrantyClaim(Guid warrantyClaimId)
    {
        if (warrantyClaimId == Guid.Empty)
            throw new ArgumentException("WarrantyClaimId cannot be empty", nameof(warrantyClaimId));

        WarrantyClaimId = warrantyClaimId;
    }

    /// <summary>
    /// Mark credit note as expired
    /// </summary>
    public void MarkAsExpired()
    {
        Status = "EXPIRED";
        Notes += "\n[Expired] Credit note has expired and can no longer be applied.";
    }

    /// <summary>
    /// Cancel the credit note
    /// </summary>
    public void Cancel(string reason = "")
    {
        if (UsedAmount > 0)
            throw new InvalidOperationException("Cannot cancel a credit note that has been partially used");

        Status = "CANCELLED";
        Notes += $"\n[Cancelled] {reason}".Trim();
    }

    /// <summary>
    /// Check if this credit note can still be applied
    /// </summary>
    public bool IsAvailable()
    {
        if (Status == "CANCELLED" || Status == "EXPIRED" || Status == "FULLY_USED")
            return false;

        if (ExpiryDate.HasValue && ExpiryDate.Value < DateTime.UtcNow)
        {
            MarkAsExpired();
            return false;
        }

        return AvailableAmount > 0;
    }
}
