namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Represents a credit note issued to a supplier for returned goods.
/// Credit notes can be applied as credit against future purchase orders from the same supplier.
/// </summary>
public class CreditNote : AuditableEntity
{
    public string CreditNoteNumber { get; private set; } = string.Empty;
    public Guid SupplierId { get; private set; }
    public Guid? PurchaseReturnId { get; private set; }  // Link to the original return
    public Guid? PurchaseOrderId { get; private set; }  // PO this credit was applied to (if any)
    public decimal TotalAmount { get; private set; }  // Original credit note amount
    public decimal UsedAmount { get; private set; } = 0;  // Amount already applied to POs
    public decimal AvailableAmount => TotalAmount - UsedAmount;  // Remaining credit available
    public string Currency { get; private set; } = "USD";
    public DateTime IssueDate { get; private set; }
    public DateTime? ExpiryDate { get; private set; }  // Optional expiry date
    public string Status { get; private set; } = "AVAILABLE";  // AVAILABLE, PARTIALLY_USED, FULLY_USED, EXPIRED, CANCELLED
    public string Notes { get; private set; } = string.Empty;
    public string IssuedBy { get; private set; } = string.Empty;

    // Navigation properties
    public Supplier? Supplier { get; set; }
    public PurchaseReturn? PurchaseReturn { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    private CreditNote() { }

    /// <summary>
    /// Create a new credit note from a purchase return
    /// </summary>
    public static CreditNote Create(
        string creditNoteNumber,
        Guid supplierId,
        Guid? purchaseReturnId,
        decimal amount,
        string currency = "USD",
        DateTime? issueDate = null,
        DateTime? expiryDate = null,
        string notes = "",
        string issuedBy = "")
    {
        if (string.IsNullOrWhiteSpace(creditNoteNumber))
            throw new ArgumentException("Credit note number cannot be empty", nameof(creditNoteNumber));

        if (supplierId == Guid.Empty)
            throw new ArgumentException("SupplierId cannot be empty", nameof(supplierId));

        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than 0", nameof(amount));

        if (expiryDate.HasValue && issueDate.HasValue && expiryDate.Value < issueDate.Value)
            throw new ArgumentException("Expiry date cannot be before issue date", nameof(expiryDate));

        return new CreditNote
        {
            CreditNoteNumber = creditNoteNumber.Trim().ToUpper(),
            SupplierId = supplierId,
            PurchaseReturnId = purchaseReturnId,
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
    /// Apply credit note amount to a purchase order
    /// </summary>
    public decimal ApplyToPurchaseOrder(Guid purchaseOrderId, decimal amountToApply)
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
        PurchaseOrderId = purchaseOrderId;

        // Update status based on usage
        if (AvailableAmount <= 0)
            Status = "FULLY_USED";
        else if (UsedAmount > 0)
            Status = "PARTIALLY_USED";

        return AvailableAmount;
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
