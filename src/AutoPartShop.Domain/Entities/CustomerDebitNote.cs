namespace AutoPartShop.Domain.Entities;

/// <summary>
/// A supplementary bill issued to a customer when an invoice undercharged them — the mirror image
/// of CustomerCreditNote. Deliberately simpler than the credit note: a credit note is a redeemable
/// asset applied against *future* invoices (hence UsedAmount/AvailableAmount tracking), whereas a
/// debit note just adds a fixed amount to what the customer already owes — there is nothing to
/// "apply" it to. Status only tracks whether it has been settled for bookkeeping purposes.
/// </summary>
public class CustomerDebitNote : AuditableEntity
{
    public string DebitNoteNumber { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public Guid? InvoiceId { get; private set; }  // The invoice this correction relates to
    public decimal TotalAmount { get; private set; }
    public string Currency { get; private set; } = "BDT";
    public DateTime IssueDate { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string Status { get; private set; } = "ISSUED";  // ISSUED, SETTLED, CANCELLED
    public string Notes { get; private set; } = string.Empty;
    public string IssuedBy { get; private set; } = string.Empty;

    // Navigation properties
    public Customer? Customer { get; set; }
    public Invoice? Invoice { get; set; }

    private CustomerDebitNote() { }

    public static CustomerDebitNote Create(
        string debitNoteNumber,
        Guid customerId,
        Guid? invoiceId,
        decimal amount,
        string reason,
        string currency = "BDT",
        DateTime? issueDate = null,
        string notes = "",
        string issuedBy = "")
    {
        if (string.IsNullOrWhiteSpace(debitNoteNumber))
            throw new ArgumentException("Debit note number cannot be empty", nameof(debitNoteNumber));

        if (customerId == Guid.Empty)
            throw new ArgumentException("CustomerId cannot be empty", nameof(customerId));

        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than 0", nameof(amount));

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason cannot be empty", nameof(reason));

        return new CustomerDebitNote
        {
            DebitNoteNumber = debitNoteNumber.Trim().ToUpper(),
            CustomerId = customerId,
            InvoiceId = invoiceId,
            TotalAmount = amount,
            Reason = reason.Trim(),
            Currency = string.IsNullOrWhiteSpace(currency) ? "BDT" : currency.Trim().ToUpper(),
            IssueDate = issueDate ?? DateTime.UtcNow,
            Status = "ISSUED",
            Notes = notes?.Trim() ?? string.Empty,
            IssuedBy = issuedBy?.Trim() ?? string.Empty
        };
    }

    public void MarkAsSettled()
    {
        if (Status != "ISSUED")
            throw new InvalidOperationException($"Only ISSUED debit notes can be settled. Current: {Status}");

        Status = "SETTLED";
    }

    public void Cancel(string reason = "")
    {
        if (Status == "SETTLED")
            throw new InvalidOperationException("Cannot cancel a settled debit note");

        Status = "CANCELLED";
        Notes = string.IsNullOrWhiteSpace(reason) ? Notes : $"{Notes}\n[Cancelled] {reason}".Trim();
    }
}
