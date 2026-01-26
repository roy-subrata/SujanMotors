namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Invoice generated from a sales order
/// </summary>
public class Invoice : AuditableEntity
{
    public string InvoiceNumber { get; private set; } = string.Empty;
    public Guid SalesOrderId { get; private set; }
    public DateTime InvoiceDate { get; private set; }
    public DateTime DueDate { get; private set; }
    public decimal SubTotal { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal DiscountAmount { get; private set; } = 0;
    public decimal GrandTotal => SubTotal + TaxAmount - DiscountAmount;
    public string Status { get; private set; } = "DRAFT";  // DRAFT, ISSUED, PAID, PARTIALLY_PAID, OVERDUE, CANCELLED
    public string Notes { get; private set; } = string.Empty;
    public string Currency { get; private set; } = "BDT";  // ISO 4217 currency code

    // Navigation properties
    public SalesOrder? SalesOrder { get; set; }
    public ICollection<CustomerPayment> CustomerPayments { get; set; } = new List<CustomerPayment>();

    // Computed properties - Single Source of Truth from CustomerPayments
    public decimal TotalAmount => GrandTotal;  // Alias for clarity
    public decimal AmountPaid =>
        CustomerPayments?
            .Where(p => p.Status == "COMPLETED")
            .Sum(p => p.Amount) ?? 0;
    public decimal OutstandingAmount => GrandTotal - AmountPaid;
    public decimal CreditBalance => AmountPaid > GrandTotal ? AmountPaid - GrandTotal : 0;
    public bool HasCredit => CreditBalance > 0;
    public bool IsOverdue => Status != "PAID" && Status != "CANCELLED" && DueDate < DateTime.UtcNow.Date;

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
            Status = "DRAFT",
            Notes = notes?.Trim() ?? string.Empty,
            Currency = string.IsNullOrWhiteSpace(currency) ? "BDT" : currency.Trim().ToUpper()
        };
    }

    public void Issue()
    {
        if (Status != "DRAFT")
            throw new InvalidOperationException("Only draft invoices can be issued");

        Status = "ISSUED";
    }

    /// <summary>
    /// Updates invoice payment status based on CustomerPayments
    /// Call this after adding/updating customer payments
    /// </summary>
    public void UpdatePaymentStatus()
    {
        var paid = AmountPaid;
        var total = GrandTotal;

        if (paid <= 0)
        {
            Status = IsOverdue ? "OVERDUE" : "ISSUED";
        }
        else if (paid >= total)
        {
            Status = "PAID";
        }
        else
        {
            Status = IsOverdue ? "OVERDUE" : "PARTIALLY_PAID";
        }
    }

    public void Cancel(string reason = "")
    {
        if (Status == "PAID")
            throw new InvalidOperationException("Cannot cancel a paid invoice");

        Status = "CANCELLED";
        Notes = reason?.Trim() ?? string.Empty;
    }

    public void SetDiscount(decimal amount)
    {
        if (amount < 0 || amount > SubTotal)
            throw new ArgumentException("Invalid discount amount", nameof(amount));

        DiscountAmount = amount;
    }

    public void UpdateNotes(string notes)
    {
        Notes = notes?.Trim() ?? string.Empty;
    }
}
