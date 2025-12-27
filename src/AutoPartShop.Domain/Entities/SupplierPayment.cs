namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Tracks payments made to suppliers
/// </summary>
public class SupplierPayment : AuditableEntity
{
    public Guid SupplierId { get; private set; }
    public Guid? PurchaseOrderId { get; private set; }  // Optional: link to specific PO
    public Guid PaymentProviderId { get; private set; }
    public string TransactionNumber { get; private set; } = string.Empty;  // Unique transaction ID
    public decimal Amount { get; private set; }
    public decimal PaymentFee { get; private set; } = 0;  // Fee charged by provider
    public decimal NetAmount { get; private set; }  // Amount - Fee
    public string Currency { get; private set; } = "USD";
    public DateTime PaymentDate { get; private set; }
    public string PaymentMethod { get; private set; } = string.Empty;  // BANK_TRANSFER, CHECK, CASH, CRYPTO, etc.
    public string Status { get; private set; } = "PENDING";  // PENDING, PROCESSING, COMPLETED, FAILED, CANCELLED, RETURNED
    public string ReferenceNumber { get; private set; } = string.Empty;  // Check number, transfer ref, etc.
    public string AuthorizationCode { get; private set; } = string.Empty;
    public string Notes { get; private set; } = string.Empty;
    public DateTime? ProcessedDate { get; private set; }  // When payment was sent
    public string ProcessedBy { get; private set; } = string.Empty;  // User who processed payment
    public DateTime? ConfirmedDate { get; private set; }  // When supplier confirmed receipt
    public string ConfirmedBy { get; private set; } = string.Empty;  // Supplier confirmation reference
    public bool IsReconciled { get; private set; } = false;
    public DateTime? ReconciledDate { get; private set; }
    public string InvoiceNumber { get; private set; } = string.Empty;  // Supplier invoice being paid
    public PaymentType PaymentType { get; private set; } = PaymentType.REGULAR;
    public string Description { get; private set; } = string.Empty;  // Additional description of payment type

    // Navigation properties
    public Supplier? Supplier { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public PaymentProvider? PaymentProvider { get; set; }

    private SupplierPayment() { }

    public static SupplierPayment Create(Guid supplierId, Guid paymentProviderId, decimal amount,
        string paymentMethod, string transactionNumber = "", string referenceNumber = "", DateTime? paymentDate = null)
    {
        if (supplierId == Guid.Empty)
            throw new ArgumentException("SupplierId cannot be empty", nameof(supplierId));

        if (paymentProviderId == Guid.Empty)
            throw new ArgumentException("PaymentProviderId cannot be empty", nameof(paymentProviderId));

        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than 0", nameof(amount));

        if (string.IsNullOrWhiteSpace(paymentMethod))
            throw new ArgumentException("PaymentMethod cannot be empty", nameof(paymentMethod));

        return new SupplierPayment
        {
            SupplierId = supplierId,
            PaymentProviderId = paymentProviderId,
            Amount = amount,
            NetAmount = amount,
            PaymentMethod = paymentMethod.Trim().ToUpper(),
            TransactionNumber = transactionNumber?.Trim() ?? string.Empty,
            ReferenceNumber = referenceNumber?.Trim() ?? string.Empty,
            PaymentDate = paymentDate ?? DateTime.UtcNow,
            Status = "PENDING"
        };
    }

    public void LinkToPurchaseOrder(Guid purchaseOrderId)
    {
        PurchaseOrderId = purchaseOrderId;
    }

    public void SetFee(decimal feeAmount)
    {
        if (feeAmount < 0)
            throw new ArgumentException("Fee amount cannot be negative", nameof(feeAmount));

        PaymentFee = feeAmount;
        NetAmount = Amount - PaymentFee;
    }

    public void SetAuthorizationCode(string code)
    {
        AuthorizationCode = code?.Trim() ?? string.Empty;
    }

    public void SetInvoiceNumber(string invoiceNumber)
    {
        InvoiceNumber = invoiceNumber?.Trim() ?? string.Empty;
    }

    public void MarkAsProcessing()
    {
        if (Status != "PENDING")
            throw new InvalidOperationException("Only pending payments can be marked as processing");
        Status = "PROCESSING";
    }

    public void MarkAsProcessed(string processedBy)
    {
        if (Status != "PROCESSING" && Status != "PENDING")
            throw new InvalidOperationException("Only pending or processing payments can be marked as processed");

        if (string.IsNullOrWhiteSpace(processedBy))
            throw new ArgumentException("ProcessedBy cannot be empty", nameof(processedBy));

        Status = "COMPLETED";
        ProcessedDate = DateTime.UtcNow;
        ProcessedBy = processedBy.Trim();
    }

    public void ConfirmReceipt(string confirmedBy)
    {
        if (Status != "COMPLETED")
            throw new InvalidOperationException("Only completed payments can be confirmed as received");

        if (string.IsNullOrWhiteSpace(confirmedBy))
            throw new ArgumentException("ConfirmedBy cannot be empty", nameof(confirmedBy));

        ConfirmedDate = DateTime.UtcNow;
        ConfirmedBy = confirmedBy.Trim();
    }

    public void MarkAsFailed()
    {
        Status = "FAILED";
    }

    public void MarkAsReturned()
    {
        if (Status != "COMPLETED")
            throw new InvalidOperationException("Only completed payments can be marked as returned");
        Status = "RETURNED";
    }

    public void Cancel()
    {
        if (Status == "COMPLETED" || Status == "RETURNED" || Status == "CANCELLED")
            throw new InvalidOperationException($"Cannot cancel a {Status} payment");
        Status = "CANCELLED";
    }

    public void Reconcile()
    {
        if (Status != "COMPLETED")
            throw new InvalidOperationException("Only completed payments can be reconciled");

        IsReconciled = true;
        ReconciledDate = DateTime.UtcNow;
    }

    public void UpdateNotes(string notes)
    {
        Notes = notes?.Trim() ?? string.Empty;
    }

    public void MarkAsAdvance()
    {
        PaymentType = PaymentType.ADVANCE;
    }

    public void MarkAsRegular()
    {
        PaymentType = PaymentType.REGULAR;
    }
}
public enum PaymentType
{
    REGULAR,
    ADVANCE
}