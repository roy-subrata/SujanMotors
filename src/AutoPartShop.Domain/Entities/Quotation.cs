namespace AutoPartShop.Domain.Entities;

/// <summary>
/// A customer-facing price quote. Distinct from SalesOrder: a quotation never affects stock or
/// payments, it only prices goods for a prospective sale. Lifecycle: DRAFT → SENT → ACCEPTED →
/// CONVERTED (to a SalesOrder), or SENT → REJECTED, or any non-terminal state → EXPIRED once
/// ValidUntil has passed.
/// </summary>
public class Quotation : AuditableEntity
{
    public string QuotationNumber { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public string CustomerEmail { get; private set; } = string.Empty;
    public string CustomerPhone { get; private set; } = string.Empty;
    public DateTime QuoteDate { get; private set; }
    public DateTime ValidUntil { get; private set; }
    public string Status { get; private set; } = "DRAFT";  // DRAFT, SENT, ACCEPTED, REJECTED, EXPIRED, CONVERTED
    public decimal SubTotal { get; private set; } = 0;
    public decimal DiscountPercentage { get; private set; } = 0;
    public decimal DiscountAmount { get; private set; } = 0;
    /// <summary>Post-discount, pre-tax — mirrors SalesOrder.TotalAmount.</summary>
    public decimal TotalAmount { get; private set; } = 0;
    public decimal TaxAmount { get; private set; } = 0;
    public decimal GrandTotal => TotalAmount + TaxAmount;
    public string Currency { get; private set; } = "BDT";
    public string Notes { get; private set; } = string.Empty;
    /// <summary>Set once the quote is accepted and converted; the quote itself becomes read-only.</summary>
    public Guid? ConvertedToSalesOrderId { get; private set; }

    // Navigation properties
    public Customer? Customer { get; set; }
    public SalesOrder? ConvertedToSalesOrder { get; set; }
    public ICollection<QuotationLine> LineItems { get; set; } = new List<QuotationLine>();

    private Quotation() { }

    public static Quotation Create(string quotationNumber, Guid customerId, string customerName,
        string customerEmail, string customerPhone, DateTime? validUntil = null,
        string notes = "", string currency = "BDT")
    {
        if (string.IsNullOrWhiteSpace(quotationNumber))
            throw new ArgumentException("QuotationNumber cannot be empty", nameof(quotationNumber));

        if (customerId == Guid.Empty)
            throw new ArgumentException("CustomerId cannot be empty", nameof(customerId));

        if (string.IsNullOrWhiteSpace(customerName))
            throw new ArgumentException("CustomerName cannot be empty", nameof(customerName));

        var quoteDate = DateTime.UtcNow;

        return new Quotation
        {
            QuotationNumber = quotationNumber.Trim().ToUpper(),
            CustomerId = customerId,
            CustomerName = customerName.Trim(),
            CustomerEmail = customerEmail?.Trim() ?? string.Empty,
            CustomerPhone = customerPhone?.Trim() ?? string.Empty,
            QuoteDate = quoteDate,
            // Handoff default is 15 days' validity when the caller doesn't specify one.
            ValidUntil = validUntil ?? quoteDate.AddDays(15),
            Notes = notes?.Trim() ?? string.Empty,
            Currency = string.IsNullOrWhiteSpace(currency) ? "BDT" : currency.Trim().ToUpper(),
            Status = "DRAFT"
        };
    }

    public void CalculateTotal()
    {
        SubTotal = LineItems.Sum(l => l.TotalPrice);

        if (DiscountPercentage < 0) DiscountPercentage = 0;
        if (DiscountPercentage > 100) DiscountPercentage = 100;

        DiscountAmount = SubTotal * (DiscountPercentage / 100);

        TotalAmount = SubTotal - DiscountAmount;
        if (TotalAmount < 0) TotalAmount = 0;
    }

    public void SetTax(decimal taxAmount)
    {
        if (taxAmount < 0)
            throw new ArgumentException("Tax amount cannot be negative", nameof(taxAmount));

        TaxAmount = taxAmount;
    }

    public void SetDiscountPercentage(decimal discountPercentage)
    {
        if (discountPercentage < 0 || discountPercentage > 100)
            throw new ArgumentException("Discount percentage must be between 0 and 100", nameof(discountPercentage));

        DiscountPercentage = discountPercentage;
    }

    public void UpdateNotes(string notes) => Notes = notes?.Trim() ?? string.Empty;

    public void UpdateValidUntil(DateTime validUntil)
    {
        if (validUntil.Date < QuoteDate.Date)
            throw new ArgumentException("ValidUntil cannot be before the quote date", nameof(validUntil));

        ValidUntil = validUntil;
    }

    public void Send()
    {
        if (Status != "DRAFT")
            throw new InvalidOperationException($"Only DRAFT quotations can be sent. Current: {Status}");

        if (!LineItems.Any())
            throw new InvalidOperationException("Quotation must have at least one line item");

        Status = "SENT";
    }

    public void Accept()
    {
        if (Status != "SENT")
            throw new InvalidOperationException($"Only SENT quotations can be accepted. Current: {Status}");

        Status = "ACCEPTED";
    }

    public void Reject(string reason = "")
    {
        if (Status != "SENT")
            throw new InvalidOperationException($"Only SENT quotations can be rejected. Current: {Status}");

        Status = "REJECTED";
        if (!string.IsNullOrWhiteSpace(reason))
            Notes = string.IsNullOrWhiteSpace(Notes) ? $"[Rejected] {reason}" : $"{Notes}\n[Rejected] {reason}";
    }

    public void MarkAsExpired()
    {
        if (Status is "CONVERTED" or "REJECTED" or "EXPIRED")
            throw new InvalidOperationException($"Cannot expire a {Status} quotation");

        Status = "EXPIRED";
    }

    /// <summary>Called once the accepted quote's lines have been copied into a new SalesOrder.</summary>
    public void MarkAsConverted(Guid salesOrderId)
    {
        if (Status != "ACCEPTED")
            throw new InvalidOperationException($"Only ACCEPTED quotations can be converted. Current: {Status}");

        if (salesOrderId == Guid.Empty)
            throw new ArgumentException("SalesOrderId cannot be empty", nameof(salesOrderId));

        Status = "CONVERTED";
        ConvertedToSalesOrderId = salesOrderId;
    }

    public bool IsExpired => Status is not ("CONVERTED" or "REJECTED" or "EXPIRED") && ValidUntil.Date < DateTime.UtcNow.Date;
}
