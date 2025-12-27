namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Payment record for an invoice
/// OBSOLETE: Use CustomerPayment as the single source of truth for all payments
/// This entity is deprecated and will be removed in a future version
/// </summary>
[Obsolete("Use CustomerPayment instead. InvoicePayment is deprecated and will be removed in a future version.")]
public class InvoicePayment : AuditableEntity
{
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;  // CASH, CHEQUE, BANK_TRANSFER, CARD, etc.
    public string ReferenceNumber { get; set; } = string.Empty;  // Cheque number, transaction ID, etc.
    public string Notes { get; set; } = string.Empty;

    // Navigation properties
    public Invoice? Invoice { get; set; }
}
