namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Records a return of goods to a supplier
/// </summary>
public class PurchaseReturn : AuditableEntity
{
    public string ReturnNumber { get; private set; } = string.Empty;
    public Guid PurchaseOrderId { get; private set; }
    public Guid SupplierId { get; private set; }
    public DateTime ReturnDate { get; private set; }
    public string Reason { get; private set; } = string.Empty;  // DAMAGED, DEFECTIVE, WRONG_ITEM, EXCESS_STOCK, QUALITY_ISSUE, etc.
    public string Status { get; private set; } = "PENDING";  // PENDING, APPROVED, RETURNED, RECEIVED, REJECTED, CREDITED
    public decimal RefundAmount { get; private set; } = 0;
    public decimal CreditNoteAmount { get; private set; } = 0;  // Amount credited by supplier
    public string Notes { get; private set; } = string.Empty;
    public string ApprovedBy { get; private set; } = string.Empty;
    public DateTime? ApprovedDate { get; private set; }
    public DateTime? ReceivedDate { get; private set; }  // When supplier received the return
    public string ReceivedBy { get; private set; } = string.Empty;

    // Settlement tracking fields - for financial reconciliation
    public string SettlementStatus { get; private set; } = "PENDING";  // PENDING, SETTLED
    public decimal SettledAmount { get; private set; } = 0;
    public DateTime? SettledDate { get; private set; }
    public string SettlementMethod { get; private set; } = string.Empty;  // CREDIT, CASH, BANK_TRANSFER
    public string SettlementNotes { get; private set; } = string.Empty;

    // Navigation properties
    public PurchaseOrder? PurchaseOrder { get; set; }
    public Supplier? Supplier { get; set; }
    public ICollection<PurchaseReturnLine> LineItems { get; set; } = new List<PurchaseReturnLine>();

    private PurchaseReturn() { }

    public static PurchaseReturn Create(string returnNumber, Guid purchaseOrderId, Guid supplierId,
        string reason, DateTime? returnDate = null, string notes = "")
    {
        if (string.IsNullOrWhiteSpace(returnNumber))
            throw new ArgumentException("ReturnNumber cannot be empty", nameof(returnNumber));

        if (purchaseOrderId == Guid.Empty)
            throw new ArgumentException("PurchaseOrderId cannot be empty", nameof(purchaseOrderId));

        if (supplierId == Guid.Empty)
            throw new ArgumentException("SupplierId cannot be empty", nameof(supplierId));

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason cannot be empty", nameof(reason));

        return new PurchaseReturn
        {
            ReturnNumber = returnNumber.Trim().ToUpper(),
            PurchaseOrderId = purchaseOrderId,
            SupplierId = supplierId,
            ReturnDate = returnDate ?? DateTime.UtcNow,
            Reason = reason.Trim(),
            Status = "PENDING",
            Notes = notes?.Trim() ?? string.Empty
        };
    }

    public void Approve(string approvedBy)
    {
        if (Status != "PENDING")
            throw new InvalidOperationException("Only pending returns can be approved");

        if (string.IsNullOrWhiteSpace(approvedBy))
            throw new ArgumentException("ApprovedBy cannot be empty", nameof(approvedBy));

        Status = "APPROVED";
        ApprovedBy = approvedBy.Trim();
        ApprovedDate = DateTime.UtcNow;
    }

    public void MarkAsReturned()
    {
        if (Status != "APPROVED")
            throw new InvalidOperationException("Only approved returns can be marked as returned");

        Status = "RETURNED";
    }

    public void MarkAsReceived(string receivedBy)
    {
        if (Status != "RETURNED")
            throw new InvalidOperationException("Only returned items can be marked as received");

        if (string.IsNullOrWhiteSpace(receivedBy))
            throw new ArgumentException("ReceivedBy cannot be empty", nameof(receivedBy));

        Status = "RECEIVED";
        ReceivedDate = DateTime.UtcNow;
        ReceivedBy = receivedBy.Trim();
    }

    public void IssueCreditNote(decimal creditAmount)
    {
        if (creditAmount <= 0)
            throw new ArgumentException("Credit amount must be greater than 0", nameof(creditAmount));

        if (creditAmount > RefundAmount)
            throw new InvalidOperationException("Credit amount cannot exceed refund amount");

        CreditNoteAmount = creditAmount;
        Status = "CREDITED";

        // Issuing a credit note IS the financial settlement (CREDIT method)
        // This reduces the supplier balance in the ledger
        SettlementStatus = "SETTLED";
        SettledAmount = creditAmount;
        SettledDate = DateTime.UtcNow;
        SettlementMethod = "CREDIT";
        SettlementNotes = $"Credit note issued for {creditAmount:C}";
    }

    public void Reject(string reason = "")
    {
        Status = "REJECTED";
        Notes = reason?.Trim() ?? string.Empty;
    }

    public void CalculateRefund()
    {
        RefundAmount = LineItems.Sum(l => l.RefundAmount);
    }

    public void UpdateNotes(string notes)
    {
        Notes = notes?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Settle the return with the supplier - records the financial settlement
    /// </summary>
    /// <param name="amount">Amount being settled</param>
    /// <param name="method">Settlement method: CREDIT, CASH, BANK_TRANSFER</param>
    /// <param name="notes">Optional settlement notes</param>
    public void SettleReturn(decimal amount, string method, string notes = "")
    {
        if (Status != "RETURNED" && Status != "RECEIVED" && Status != "CREDITED")
            throw new InvalidOperationException("Only returned, received, or credited returns can be settled");

        if (amount <= 0)
            throw new ArgumentException("Settlement amount must be greater than 0", nameof(amount));

        if (amount > RefundAmount)
            throw new InvalidOperationException("Settlement amount cannot exceed refund amount");

        if (string.IsNullOrWhiteSpace(method))
            throw new ArgumentException("Settlement method cannot be empty", nameof(method));

        SettlementStatus = "SETTLED";
        SettledAmount = amount;
        SettledDate = DateTime.UtcNow;
        SettlementMethod = method.Trim().ToUpper();
        SettlementNotes = notes?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Check if this return has been financially settled
    /// </summary>
    public bool IsSettled => SettlementStatus == "SETTLED";
}
