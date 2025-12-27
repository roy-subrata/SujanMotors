namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Tracks payments received from customers
/// </summary>
public class CustomerPayment : AuditableEntity
{
    public Guid CustomerId { get; private set; }
    public Guid? InvoiceId { get; private set; }  // Optional: link to specific invoice
    public Guid? PaymentProviderId { get; private set; }  // Optional: payment provider
    public string TransactionNumber { get; private set; } = string.Empty;  // Unique transaction ID
    public decimal Amount { get; private set; }
    public decimal PaymentFee { get; private set; } = 0;  // Fee charged by provider
    public decimal NetAmount { get; private set; }  // Amount - Fee
    public string Currency { get; private set; } = "USD";
    public DateTime PaymentDate { get; private set; }
    public string PaymentMethod { get; private set; } = string.Empty;  // CREDIT_CARD, BANK_TRANSFER, CHECK, CASH, etc.
    public string Status { get; private set; } = "PENDING";  // PENDING, PROCESSING, COMPLETED, FAILED, REFUNDED, CANCELLED
    public string ReferenceNumber { get; private set; } = string.Empty;  // Check number, transfer ref, card last 4, etc.
    public string AuthorizationCode { get; private set; } = string.Empty;
    public string Notes { get; private set; } = string.Empty;
    public DateTime? SettledDate { get; private set; }  // When payment cleared
    public string SettledBy { get; private set; } = string.Empty;  // User who confirmed settlement
    public bool IsReconciled { get; private set; } = false;
    public DateTime? ReconciledDate { get; private set; }

    // Navigation properties
    public Customer? Customer { get; set; }
    public Invoice? Invoice { get; set; }
    public PaymentProvider? PaymentProvider { get; set; }

    private CustomerPayment() { }

    public static CustomerPayment Create(Guid customerId, Guid? paymentProviderId, decimal amount,
        string paymentMethod, string transactionNumber = "", string referenceNumber = "", DateTime? paymentDate = null)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("CustomerId cannot be empty", nameof(customerId));

        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than 0", nameof(amount));

        if (string.IsNullOrWhiteSpace(paymentMethod))
            throw new ArgumentException("PaymentMethod cannot be empty", nameof(paymentMethod));

        return new CustomerPayment
        {
            CustomerId = customerId,
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

    public void LinkToInvoice(Guid invoiceId)
    {
        InvoiceId = invoiceId;
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

    public void MarkAsProcessing()
    {
        if (Status != "PENDING")
            throw new InvalidOperationException("Only pending payments can be marked as processing");
        Status = "PROCESSING";
    }

    public void MarkAsCompleted()
    {
        if (Status != "PROCESSING" && Status != "PENDING")
            throw new InvalidOperationException("Only pending or processing payments can be completed");
        Status = "COMPLETED";
    }

    public void MarkAsSettled(string settledBy)
    {
        if (string.IsNullOrWhiteSpace(settledBy))
            throw new ArgumentException("SettledBy cannot be empty", nameof(settledBy));

        Status = "COMPLETED";
        SettledDate = DateTime.UtcNow;
        SettledBy = settledBy.Trim();
    }

    public void MarkAsFailed()
    {
        Status = "FAILED";
    }

    public void MarkAsRefunded(decimal refundAmount)
    {
        if (refundAmount <= 0)
            throw new ArgumentException("Refund amount must be greater than 0", nameof(refundAmount));

        if (refundAmount > Amount)
            throw new InvalidOperationException("Refund amount cannot exceed payment amount");

        Status = "REFUNDED";
    }

    public void Cancel()
    {
        if (Status == "COMPLETED" || Status == "REFUNDED")
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

    public void UpdateReferenceNumber(string referenceNumber)
    {
        ReferenceNumber = referenceNumber?.Trim() ?? string.Empty;
    }
}
