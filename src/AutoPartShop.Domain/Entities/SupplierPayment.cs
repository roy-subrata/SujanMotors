using AutoPartShop.Domain.Enums;

namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Tracks payments made to suppliers
/// </summary>
public class SupplierPayment : AuditableEntity
{
    /// <summary>Optimistic-concurrency token (SQL Server rowversion).</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Guid SupplierId { get; private set; }
    public Guid? PurchaseOrderId { get; private set; }  // Optional: link to specific PO
    public Guid? GoodsReceiptId { get; private set; }  // Optional: link to specific GR
    public Guid PaymentProviderId { get; private set; }
    public Guid? SupplierPaymentAccountId { get; private set; }  // Supplier's payment account (where to send money)
    public string TransactionNumber { get; private set; } = string.Empty;  // Unique transaction ID
    public decimal Amount { get; private set; }
    public decimal PaymentFee { get; private set; } = 0;  // Fee charged by provider
    public decimal NetAmount { get; private set; }  // Amount - Fee
    public string Currency { get; private set; } = "BDT";
    public decimal? BaseAmount { get; private set; }  // Amount converted to base currency at payment time
    public decimal? FxRateToBase { get; private set; }  // Exchange rate applied when BaseAmount was captured (1 = same as base)
    public DateTime PaymentDate { get; private set; }
    public string PaymentMethod { get; private set; } = string.Empty;  // BANK_TRANSFER, CHECK, CASH, CRYPTO, etc.
    public SupplierPaymentStatus Status { get; private set; } = SupplierPaymentStatus.PENDING;
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
    public decimal RemainingAmount { get; private set; } = 0;  // For ADVANCE payments: tracks unused balance
    public Guid? SourceAdvancePaymentId { get; private set; }  // For REGULAR payments: links to advance payment used

    // Navigation properties
    public Supplier? Supplier { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public GoodsReceipt? GoodsReceipt { get; set; }
    public PaymentProvider? PaymentProvider { get; set; }
    public SupplierPaymentAccount? SupplierPaymentAccount { get; set; }  // Supplier's payment account (destination)
    public SupplierPayment? SourceAdvancePayment { get; set; }  // The advance payment this payment is applied from

    private SupplierPayment() { }

    public static SupplierPayment Create(Guid supplierId, Guid paymentProviderId, decimal amount,
        string paymentMethod, string transactionNumber = "", string referenceNumber = "", DateTime? paymentDate = null,
        string currency = "BDT")
    {
        if (supplierId == Guid.Empty)
            throw new ArgumentException("SupplierId cannot be empty", nameof(supplierId));

        if (paymentProviderId == Guid.Empty)
            throw new ArgumentException("PaymentProviderId cannot be empty", nameof(paymentProviderId));

        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than 0", nameof(amount));

        if (string.IsNullOrWhiteSpace(paymentMethod))
            throw new ArgumentException("PaymentMethod cannot be empty", nameof(paymentMethod));

        // Generate unique transaction number if not provided
        var finalTransactionNumber = string.IsNullOrWhiteSpace(transactionNumber)
            ? $"TXN-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString()[..8]}"
            : transactionNumber.Trim();

        return new SupplierPayment
        {
            SupplierId = supplierId,
            PaymentProviderId = paymentProviderId,
            Amount = amount,
            NetAmount = amount,
            PaymentMethod = paymentMethod.Trim().ToUpper(),
            TransactionNumber = finalTransactionNumber,
            ReferenceNumber = referenceNumber?.Trim() ?? string.Empty,
            PaymentDate = paymentDate ?? DateTime.UtcNow,
            Currency = string.IsNullOrWhiteSpace(currency) ? "BDT" : currency.Trim().ToUpper(),
            Status = SupplierPaymentStatus.PENDING
        };
    }

    public void LinkToPurchaseOrder(Guid purchaseOrderId)
    {
        PurchaseOrderId = purchaseOrderId;
    }

    public void LinkToGoodsReceipt(Guid goodsReceiptId)
    {
        GoodsReceiptId = goodsReceiptId;
    }

    public void SetSupplierPaymentAccount(Guid? supplierPaymentAccountId)
    {
        SupplierPaymentAccountId = supplierPaymentAccountId;
    }

    public void SetFee(decimal feeAmount)
    {
        if (feeAmount < 0)
            throw new ArgumentException("Fee amount cannot be negative", nameof(feeAmount));

        PaymentFee = feeAmount;
        NetAmount = Amount - PaymentFee;
    }

    /// <summary>
    /// Captures the base-currency equivalent of <see cref="Amount"/> at payment time so balances
    /// and reports stay stable even if exchange rates change later.
    /// </summary>
    public void SetFxBaseAmount(decimal baseAmount, decimal rateToBase)
    {
        if (baseAmount < 0)
            throw new ArgumentException("Base amount cannot be negative", nameof(baseAmount));

        if (rateToBase <= 0)
            throw new ArgumentException("Rate to base must be greater than 0", nameof(rateToBase));

        BaseAmount = baseAmount;
        FxRateToBase = rateToBase;
    }

    public void SetAuthorizationCode(string code)
    {
        AuthorizationCode = code?.Trim() ?? string.Empty;
    }

    public void SetInvoiceNumber(string invoiceNumber)
    {
        InvoiceNumber = invoiceNumber?.Trim() ?? string.Empty;
    }

    public void SetReferenceNumber(string referenceNumber)
    {
        ReferenceNumber = referenceNumber?.Trim() ?? string.Empty;
    }

    public void MarkAsProcessing()
    {
        if (Status != SupplierPaymentStatus.PENDING)
            throw new InvalidOperationException("Only pending payments can be marked as processing");
        Status = SupplierPaymentStatus.PROCESSING;
    }

    public void MarkAsProcessed(string processedBy)
    {
        if (Status != SupplierPaymentStatus.PROCESSING && Status != SupplierPaymentStatus.PENDING)
            throw new InvalidOperationException("Only pending or processing payments can be marked as processed");

        if (string.IsNullOrWhiteSpace(processedBy))
            throw new ArgumentException("ProcessedBy cannot be empty", nameof(processedBy));

        Status = SupplierPaymentStatus.COMPLETED;
        ProcessedDate = DateTime.UtcNow;
        ProcessedBy = processedBy.Trim();
    }

    public void ConfirmReceipt(string confirmedBy)
    {
        if (Status != SupplierPaymentStatus.COMPLETED)
            throw new InvalidOperationException("Only completed payments can be confirmed as received");

        if (string.IsNullOrWhiteSpace(confirmedBy))
            throw new ArgumentException("ConfirmedBy cannot be empty", nameof(confirmedBy));

        ConfirmedDate = DateTime.UtcNow;
        ConfirmedBy = confirmedBy.Trim();
    }

    public void MarkAsFailed()
    {
        Status = SupplierPaymentStatus.FAILED;
    }

    public void MarkAsReturned()
    {
        if (Status != SupplierPaymentStatus.COMPLETED)
            throw new InvalidOperationException("Only completed payments can be marked as returned");
        Status = SupplierPaymentStatus.RETURNED;
    }

    public void Cancel()
    {
        if (Status == SupplierPaymentStatus.COMPLETED || Status == SupplierPaymentStatus.RETURNED || Status == SupplierPaymentStatus.CANCELLED)
            throw new InvalidOperationException($"Cannot cancel a {Status} payment");
        Status = SupplierPaymentStatus.CANCELLED;
    }

    public void Reconcile()
    {
        if (Status != SupplierPaymentStatus.COMPLETED)
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
        RemainingAmount = Amount;  // Initially, all of the advance is available
    }

    public void MarkAsRegular()
    {
        PaymentType = PaymentType.REGULAR;
        RemainingAmount = 0;  // Regular payments don't have remaining amounts
    }

    public void LinkToSourceAdvance(Guid sourceAdvancePaymentId)
    {
        if (PaymentType != PaymentType.REGULAR)
            throw new InvalidOperationException("Only regular payments can be linked to an advance payment");

        SourceAdvancePaymentId = sourceAdvancePaymentId;
    }

    public void ReduceRemainingAmount(decimal amountUsed)
    {
        if (PaymentType != PaymentType.ADVANCE)
            throw new InvalidOperationException("Only advance payments have remaining amounts");

        if (amountUsed <= 0)
            throw new ArgumentException("Amount used must be greater than 0", nameof(amountUsed));

        if (amountUsed > RemainingAmount)
            throw new InvalidOperationException($"Cannot use {amountUsed} from advance. Only {RemainingAmount} remaining.");

        RemainingAmount -= amountUsed;
    }

    /// <summary>
    /// Restores balance to an ADVANCE payment when the REGULAR payment that consumed it is reversed
    /// (the money comes back from the supplier, so the advance is available again). Opposite of
    /// <see cref="ReduceRemainingAmount"/>.
    /// </summary>
    public void RestoreRemainingAmount(decimal amountRestored)
    {
        if (PaymentType != PaymentType.ADVANCE)
            throw new InvalidOperationException("Only advance payments have remaining amounts");

        if (amountRestored <= 0)
            throw new ArgumentException("Amount restored must be greater than 0", nameof(amountRestored));

        if (RemainingAmount + amountRestored > Amount)
            throw new InvalidOperationException($"Cannot restore {amountRestored} to advance. Only {Amount - RemainingAmount} was consumed.");

        RemainingAmount += amountRestored;
    }

    public bool HasRemainingBalance()
    {
        return PaymentType == PaymentType.ADVANCE && RemainingAmount > 0;
    }

    /// <summary>
    /// Creates a payment transaction from applying advance credit to a purchase order
    /// </summary>
    public static SupplierPayment CreateFromAdvance(
        Guid supplierId,
        Guid purchaseOrderId,
        Guid sourceAdvancePaymentId,
        Guid paymentProviderId,
        decimal amount,
        string description,
        string currency = "BDT")
    {
        if (supplierId == Guid.Empty)
            throw new ArgumentException("SupplierId cannot be empty", nameof(supplierId));

        if (purchaseOrderId == Guid.Empty)
            throw new ArgumentException("PurchaseOrderId cannot be empty", nameof(purchaseOrderId));

        if (sourceAdvancePaymentId == Guid.Empty)
            throw new ArgumentException("SourceAdvancePaymentId cannot be empty", nameof(sourceAdvancePaymentId));

        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than 0", nameof(amount));

        var transactionNumber = $"ADV-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString()[..8]}";

        return new SupplierPayment
        {
            SupplierId = supplierId,
            PurchaseOrderId = purchaseOrderId,
            SourceAdvancePaymentId = sourceAdvancePaymentId,
            PaymentProviderId = paymentProviderId,
            Amount = amount,
            NetAmount = amount,
            PaymentMethod = "ADVANCE_CREDIT",
            TransactionNumber = transactionNumber,
            PaymentDate = DateTime.UtcNow,
            Status = SupplierPaymentStatus.COMPLETED,  // Applied advances are immediately completed
            PaymentType = PaymentType.REGULAR,
            Description = description.Trim(),
            RemainingAmount = 0,
            Currency = string.IsNullOrWhiteSpace(currency) ? "BDT" : currency.Trim().ToUpper()
        };
    }
}
public enum PaymentType
{
    REGULAR,
    ADVANCE
}