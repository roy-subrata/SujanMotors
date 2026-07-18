namespace AutoPartShop.Domain.Entities;

/// <summary>
/// A pre-payment bill sent to a customer ahead of a Tax Invoice, requesting an advance against a
/// confirmed SalesOrder. Deliberately carries no line items of its own — pricing, quantities, and
/// totals are read live from the linked SalesOrder at render time, so the two can never drift out
/// of sync. Only the document's own identity (number, issue date, validity) is persisted, since
/// that is what a customer might ask to have reissued or re-sent.
/// </summary>
public class ProformaInvoice : AuditableEntity
{
    public string ProformaNumber { get; private set; } = string.Empty;
    public Guid SalesOrderId { get; private set; }
    public DateTime IssueDate { get; private set; }
    public DateTime ValidUntil { get; private set; }
    public string Status { get; private set; } = "ISSUED";  // ISSUED, EXPIRED, SUPERSEDED
    public string IssuedBy { get; private set; } = string.Empty;
    public string Notes { get; private set; } = string.Empty;

    // Navigation properties
    public SalesOrder? SalesOrder { get; set; }

    private ProformaInvoice() { }

    public static ProformaInvoice Create(
        string proformaNumber,
        Guid salesOrderId,
        DateTime? validUntil = null,
        string issuedBy = "",
        string notes = "")
    {
        if (string.IsNullOrWhiteSpace(proformaNumber))
            throw new ArgumentException("Proforma number cannot be empty", nameof(proformaNumber));

        if (salesOrderId == Guid.Empty)
            throw new ArgumentException("SalesOrderId cannot be empty", nameof(salesOrderId));

        var issueDate = DateTime.UtcNow;

        return new ProformaInvoice
        {
            ProformaNumber = proformaNumber.Trim().ToUpper(),
            SalesOrderId = salesOrderId,
            IssueDate = issueDate,
            // Handoff default is 15 days' validity when the caller doesn't specify one.
            ValidUntil = validUntil ?? issueDate.AddDays(15),
            Status = "ISSUED",
            IssuedBy = issuedBy?.Trim() ?? string.Empty,
            Notes = notes?.Trim() ?? string.Empty
        };
    }

    public void MarkAsExpired()
    {
        if (Status != "ISSUED")
            throw new InvalidOperationException($"Only ISSUED proforma invoices can expire. Current: {Status}");

        Status = "EXPIRED";
    }

    /// <summary>A reissued proforma (e.g. after a price change on the order) supersedes this one.</summary>
    public void MarkAsSuperseded()
    {
        if (Status != "ISSUED")
            throw new InvalidOperationException($"Only ISSUED proforma invoices can be superseded. Current: {Status}");

        Status = "SUPERSEDED";
    }

    public bool IsExpired => Status == "ISSUED" && ValidUntil.Date < DateTime.UtcNow.Date;
}
