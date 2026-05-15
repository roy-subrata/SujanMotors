
namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Records a return of goods from a customer
/// </summary>
public class SalesReturn : AuditableEntity
{
    public string ReturnNumber { get; private set; } = string.Empty;
    public Guid SalesOrderId { get; private set; }
    public Guid? InvoiceId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public DateTime ReturnDate { get; private set; }
    public string Reason { get; private set; } = string.Empty;  // DAMAGED, DEFECTIVE, WRONG_ITEM, EXCESS_STOCK, etc.
    public string Status { get; private set; } = "PENDING";  // PENDING, APPROVED, RECEIVED, REJECTED, PROCESSED
    public decimal RefundAmount { get; private set; } = 0;
    public string RefundType { get; private set; } = "CASH_REFUND";  // CASH_REFUND, STORE_CREDIT
    public string Notes { get; private set; } = string.Empty;
    public string ApprovedBy { get; private set; } = string.Empty;
    public DateTime? ApprovedDate { get; private set; }

    // Credit note tracking
    public Guid? CustomerCreditNoteId { get; private set; }

    // Navigation properties
    public SalesOrder? SalesOrder { get; set; }
    public Invoice? Invoice { get; set; }
    public CustomerCreditNote? CustomerCreditNote { get; set; }
    public ICollection<SalesReturnLine> LineItems { get; set; } = new List<SalesReturnLine>();

    private SalesReturn() { }

    public static SalesReturn Create(string returnNumber, Guid salesOrderId, Guid? invoiceId,
        string reason, Guid warehouseId, DateTime? returnDate = null, string notes = "")
    {
        if (string.IsNullOrWhiteSpace(returnNumber))
            throw new ArgumentException("ReturnNumber cannot be empty", nameof(returnNumber));

        if (salesOrderId == Guid.Empty)
            throw new ArgumentException("SalesOrderId cannot be empty", nameof(salesOrderId));

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason cannot be empty", nameof(reason));

        if (warehouseId == Guid.Empty)
            throw new ArgumentException("WarehouseId cannot be empty", nameof(warehouseId));

        return new SalesReturn
        {
            ReturnNumber = returnNumber.Trim().ToUpper(),
            SalesOrderId = salesOrderId,
            InvoiceId = invoiceId,
            WarehouseId = warehouseId,
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

    public void MarkAsReceived()
    {
        if (Status != "APPROVED")
            throw new InvalidOperationException("Only approved returns can be marked as received");

        Status = "RECEIVED";
    }

    public void Reject(string reason = "")
    {
        Status = "REJECTED";
        var trimmedReason = reason?.Trim() ?? string.Empty;
        if (!string.IsNullOrEmpty(trimmedReason))
        {
            Notes = string.IsNullOrEmpty(Notes)
                ? $"Rejected: {trimmedReason}"
                : $"{Notes} | Rejected: {trimmedReason}";
        }
    }

    public void Process()
    {
        if (Status != "RECEIVED")
            throw new InvalidOperationException("Only received returns can be processed");

        Status = "PROCESSED";
    }

    public void CalculateRefund()
    {
        RefundAmount = LineItems.Sum(l => l.RefundAmount);
    }

    public void UpdateNotes(string notes)
    {
        Notes = notes?.Trim() ?? string.Empty;
    }

    public void SetRefundType(string refundType)
    {
        var validTypes = new[] { "CASH_REFUND", "STORE_CREDIT" };
        if (!validTypes.Contains(refundType?.ToUpper()))
            throw new ArgumentException($"RefundType must be one of: {string.Join(", ", validTypes)}", nameof(refundType));

        RefundType = refundType.ToUpper();
    }

    /// <summary>
    /// Link a customer credit note to this return
    /// </summary>
    public void SetCustomerCreditNote(Guid customerCreditNoteId)
    {
        if (customerCreditNoteId == Guid.Empty)
            throw new ArgumentException("Customer credit note ID cannot be empty", nameof(customerCreditNoteId));

        CustomerCreditNoteId = customerCreditNoteId;
    }

}
