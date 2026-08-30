using AutoPartShop.Domain.Enums;

namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Invoice generated from a sales order
/// </summary>
public class Invoice : AuditableEntity
{
    /// <summary>Optimistic-concurrency token (SQL Server rowversion).</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public string InvoiceNumber { get; private set; } = string.Empty;
    public Guid SalesOrderId { get; private set; }
    public DateTime InvoiceDate { get; private set; }
    public DateTime DueDate { get; private set; }
    public decimal SubTotal { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal DiscountAmount { get; private set; } = 0;

    /// <summary>
    /// Value credited back to the customer by processed sales returns. Kept separate from
    /// <see cref="DiscountAmount"/> so reports can still tell a sales discount apart from a
    /// return, while the invoice total reflects what the customer actually owes.
    /// </summary>
    public decimal ReturnedAmount { get; private set; } = 0;

    public decimal GrandTotal => SubTotal + TaxAmount - DiscountAmount - ReturnedAmount;
    public InvoiceStatus Status { get; private set; } = InvoiceStatus.DRAFT;
    public string Notes { get; private set; } = string.Empty;
    public string Currency { get; private set; } = "BDT";  // ISO 4217 currency code
    public decimal? BaseGrandTotal { get; private set; }  // GrandTotal converted to base currency at invoice time
    public decimal? FxRateToBase { get; private set; }  // Exchange rate applied when BaseGrandTotal was captured (1 = same as base)

    // Navigation properties
    public SalesOrder? SalesOrder { get; set; }
    public ICollection<CustomerPayment> CustomerPayments { get; set; } = new List<CustomerPayment>();

    // Computed properties - Single Source of Truth from CustomerPayments
    public decimal TotalAmount => GrandTotal;  // Alias for clarity
    public decimal AmountPaid =>
        CustomerPayments?
            .Where(p => p.Status == CustomerPaymentStatus.COMPLETED)
            .Sum(p => p.Amount) ?? 0;
    public decimal OutstandingAmount => GrandTotal - AmountPaid;
    public decimal CreditBalance => AmountPaid > GrandTotal ? AmountPaid - GrandTotal : 0;
    public bool HasCredit => CreditBalance > 0;
    public bool IsOverdue => Status != InvoiceStatus.PAID && Status != InvoiceStatus.CANCELLED && DueDate < DateTime.UtcNow.Date;

    private Invoice() { }

    public static Invoice Create(string invoiceNumber, Guid salesOrderId,
        decimal subTotal, decimal taxAmount, DateTime? dueDate = null, string notes = "", string currency = "BDT")
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
            throw new ArgumentException("InvoiceNumber cannot be empty", nameof(invoiceNumber));

        if (salesOrderId == Guid.Empty)
            throw new ArgumentException("SalesOrderId cannot be empty", nameof(salesOrderId));

        if (subTotal <= 0)
            throw new ArgumentException("SubTotal must be greater than 0", nameof(subTotal));

        if (taxAmount < 0)
            throw new ArgumentException("TaxAmount cannot be negative", nameof(taxAmount));

        return new Invoice
        {
            InvoiceNumber = invoiceNumber.Trim().ToUpper(),
            SalesOrderId = salesOrderId,
            InvoiceDate = DateTime.UtcNow,
            DueDate = dueDate ?? DateTime.UtcNow.AddDays(30),
            SubTotal = subTotal,
            TaxAmount = taxAmount,
            Status = InvoiceStatus.DRAFT,
            Notes = notes?.Trim() ?? string.Empty,
            Currency = string.IsNullOrWhiteSpace(currency) ? "BDT" : currency.Trim().ToUpper()
        };
    }

    public void Issue()
    {
        if (Status != InvoiceStatus.DRAFT)
            throw new InvalidOperationException("Only draft invoices can be issued");

        Status = InvoiceStatus.ISSUED;
    }

    /// <summary>
    /// Updates invoice payment status based on CustomerPayments
    /// Call this after adding/updating customer payments
    /// </summary>
    public void UpdatePaymentStatus()
    {
        if (Status == InvoiceStatus.CANCELLED)
            return;

        var paid = AmountPaid;
        var total = GrandTotal;

        if (paid <= 0)
        {
            if (IsOverdue)
                Status = InvoiceStatus.OVERDUE;
            else if (Status != InvoiceStatus.DUE)
                Status = InvoiceStatus.ISSUED;
            // else: keep DUE — balance went to zero but due date hasn't been crossed
        }
        else if (paid >= total)
        {
            Status = InvoiceStatus.PAID;
        }
        else
        {
            Status = IsOverdue ? InvoiceStatus.OVERDUE : InvoiceStatus.PARTIALLY_PAID;
        }
    }

    public void MarkAsDue()
    {
        if (Status != InvoiceStatus.ISSUED)
            throw new InvalidOperationException("Only issued invoices can be marked as due.");
        Status = InvoiceStatus.DUE;
    }

    public void Cancel(string reason = "")
    {
        if (Status == InvoiceStatus.PAID)
            throw new InvalidOperationException("Cannot cancel a paid invoice");

        if (Status == InvoiceStatus.PARTIALLY_PAID)
            throw new InvalidOperationException("Cannot cancel a partially paid invoice. Refund or reverse the payments first.");

        Status = InvoiceStatus.CANCELLED;
        Notes = reason?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Credits a processed sales return against this invoice so the payable drops by the
    /// refunded value. Without this the invoice keeps its full total while the refund lowers
    /// AmountPaid, making a fully-settled customer look like they owe the returned amount.
    /// </summary>
    public void ApplyReturnCredit(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Return credit must be greater than 0", nameof(amount));

        if (ReturnedAmount + amount > SubTotal + TaxAmount - DiscountAmount)
            throw new InvalidOperationException("Return credit cannot exceed the invoice total");

        ReturnedAmount += amount;
    }

    public void SetDiscount(decimal amount)
    {
        if (amount < 0 || amount > SubTotal)
            throw new ArgumentException("Invalid discount amount", nameof(amount));

        DiscountAmount = amount;
    }

    /// <summary>
    /// Captures the base-currency equivalent of <see cref="GrandTotal"/> at issue time so reports
    /// stay stable even if exchange rates change later.
    /// </summary>
    public void SetFxBaseAmount(decimal baseAmount, decimal rateToBase)
    {
        if (baseAmount < 0)
            throw new ArgumentException("Base amount cannot be negative", nameof(baseAmount));

        if (rateToBase <= 0)
            throw new ArgumentException("Rate to base must be greater than 0", nameof(rateToBase));

        BaseGrandTotal = baseAmount;
        FxRateToBase = rateToBase;
    }

    public void UpdateNotes(string notes)
    {
        Notes = notes?.Trim() ?? string.Empty;
    }
}
