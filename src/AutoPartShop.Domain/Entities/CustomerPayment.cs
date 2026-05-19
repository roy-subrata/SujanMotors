namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Type of customer payment
/// </summary>
public enum CustomerPaymentType
{
    REGULAR = 0,  // Normal payment linked to an invoice
    ADVANCE = 1   // Advance payment (not linked to invoice initially)
}

/// <summary>
/// Tracks payments received from customers
/// </summary>
public class CustomerPayment : AuditableEntity
{
    public Guid CustomerId { get; private set; }
    public Guid? WarrantyClaimId { get; private set; }  // Optional: link to warranty refund claim
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

    // Advance payment tracking
    public CustomerPaymentType PaymentType { get; private set; } = CustomerPaymentType.REGULAR;
    public decimal RemainingAmount { get; private set; } = 0;  // For ADVANCE payments: tracks unused balance
    public Guid? SourceAdvancePaymentId { get; private set; }  // For REGULAR payments: links to advance payment used

    // Navigation properties
    public Customer? Customer { get; set; }
    public Invoice? Invoice { get; set; }
    public PaymentProvider? PaymentProvider { get; set; }
    public CustomerPayment? SourceAdvancePayment { get; set; }  // Self-referencing navigation

    private CustomerPayment() { }

    public static CustomerPayment Create(Guid customerId, Guid? paymentProviderId, decimal amount,
        string paymentMethod, string transactionNumber = "", string referenceNumber = "", DateTime? paymentDate = null)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("CustomerId cannot be empty", nameof(customerId));

        // Allow negative amounts for refunds (REFUND payment method)
        var method = paymentMethod?.Trim().ToUpper() ?? "";
        if (amount == 0)
            throw new ArgumentException("Amount cannot be zero", nameof(amount));

        if (amount < 0 && method != "REFUND")
            throw new ArgumentException("Negative amounts are only allowed for REFUND payment method", nameof(amount));

        if (string.IsNullOrWhiteSpace(paymentMethod))
            throw new ArgumentException("PaymentMethod cannot be empty", nameof(paymentMethod));

        // Auto-generate transaction number if not provided
        var txnNumber = string.IsNullOrWhiteSpace(transactionNumber)
            ? $"CPAY-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString()[..8].ToUpper()}"
            : transactionNumber.Trim();

        return new CustomerPayment
        {
            CustomerId = customerId,
            PaymentProviderId = paymentProviderId,
            Amount = amount,
            NetAmount = amount,
            PaymentMethod = method,
            TransactionNumber = txnNumber,
            ReferenceNumber = referenceNumber?.Trim() ?? string.Empty,
            PaymentDate = paymentDate ?? DateTime.UtcNow,
            Status = "PENDING"
        };
    }

    public void LinkToInvoice(Guid invoiceId)
    {
        InvoiceId = invoiceId;
    }

    public void LinkToWarrantyClaim(Guid warrantyClaimId)
    {
        if (warrantyClaimId == Guid.Empty)
            throw new ArgumentException("WarrantyClaimId cannot be empty", nameof(warrantyClaimId));

        WarrantyClaimId = warrantyClaimId;
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

    // Advance Payment Methods

    /// <summary>
    /// Mark this payment as an advance payment
    /// </summary>
    public void MarkAsAdvance()
    {
        PaymentType = CustomerPaymentType.ADVANCE;
        RemainingAmount = Amount;  // Initially, all of the advance is available
    }

    /// <summary>
    /// Mark this payment as a regular payment
    /// </summary>
    public void MarkAsRegular()
    {
        if (PaymentType == CustomerPaymentType.ADVANCE && RemainingAmount > 0)
            throw new InvalidOperationException($"Cannot mark as regular: advance payment still has {RemainingAmount} remaining balance. Use or refund the remaining amount first.");

        PaymentType = CustomerPaymentType.REGULAR;
        RemainingAmount = 0;  // Regular payments don't have remaining amounts
    }

    /// <summary>
    /// Reduce the remaining amount of an advance payment
    /// </summary>
    public void ReduceRemainingAmount(decimal amountUsed)
    {
        if (PaymentType != CustomerPaymentType.ADVANCE)
            throw new InvalidOperationException("Only advance payments have remaining amounts");

        if (amountUsed <= 0)
            throw new ArgumentException("Amount used must be greater than zero", nameof(amountUsed));

        if (amountUsed > RemainingAmount)
            throw new InvalidOperationException($"Cannot use {amountUsed} from advance. Only {RemainingAmount} remaining.");

        RemainingAmount -= amountUsed;
    }

    /// <summary>
    /// Check if this advance payment has remaining balance
    /// </summary>
    public bool HasRemainingBalance()
    {
        return PaymentType == CustomerPaymentType.ADVANCE && RemainingAmount > 0;
    }

    /// <summary>
    /// Create a new payment from advance credit
    /// </summary>
    public static CustomerPayment CreateFromAdvance(
        Guid customerId,
        Guid invoiceId,
        Guid sourceAdvancePaymentId,
        Guid? paymentProviderId,
        decimal amount,
        string description)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("CustomerId cannot be empty", nameof(customerId));

        if (invoiceId == Guid.Empty)
            throw new ArgumentException("InvoiceId cannot be empty", nameof(invoiceId));

        if (sourceAdvancePaymentId == Guid.Empty)
            throw new ArgumentException("SourceAdvancePaymentId cannot be empty", nameof(sourceAdvancePaymentId));

        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));

        var transactionNumber = $"ADV-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString()[..8]}";

        return new CustomerPayment
        {
            CustomerId = customerId,
            InvoiceId = invoiceId,
            SourceAdvancePaymentId = sourceAdvancePaymentId,
            PaymentProviderId = paymentProviderId,
            Amount = amount,
            NetAmount = amount,
            PaymentMethod = "ADVANCE_CREDIT",
            TransactionNumber = transactionNumber,
            PaymentDate = DateTime.UtcNow,
            Status = "COMPLETED",
            PaymentType = CustomerPaymentType.REGULAR,
            Notes = description.Trim(),
            RemainingAmount = 0
        };
    }
}
