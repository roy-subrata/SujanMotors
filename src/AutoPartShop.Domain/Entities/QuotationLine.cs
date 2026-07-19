namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Line item in a quotation. Simpler than SalesOrderLine: a quote never ships and never moves
/// stock, so it carries no shipped-quantity or base-unit tracking — those are computed only if
/// and when the quote converts to a SalesOrder.
/// </summary>
public class QuotationLine : AuditableEntity
{
    public Guid QuotationId { get; private set; }
    public Guid PartId { get; private set; }
    public Guid? ProductVariantId { get; private set; }
    public Guid? UnitId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal Discount { get; private set; } = 0;  // Discount per unit
    public decimal TotalPrice => (Quantity * UnitPrice) - (Quantity * Discount);
    public string Description { get; private set; } = string.Empty;
    public int LineNumber { get; private set; }

    // Navigation properties
    public Quotation? Quotation { get; set; }
    public Product? Part { get; set; }
    public ProductVariant? ProductVariant { get; set; }
    public Unit? Unit { get; set; }

    private QuotationLine() { }

    public static QuotationLine Create(Guid quotationId, Guid partId, int quantity,
        decimal unitPrice, int lineNumber, Guid? unitId = null,
        decimal discount = 0, string description = "", Guid? productVariantId = null)
    {
        if (quotationId == Guid.Empty)
            throw new ArgumentException("QuotationId cannot be empty", nameof(quotationId));

        if (partId == Guid.Empty)
            throw new ArgumentException("PartId cannot be empty", nameof(partId));

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));

        if (unitPrice <= 0)
            throw new ArgumentException("UnitPrice must be greater than 0", nameof(unitPrice));

        if (discount < 0 || discount >= unitPrice)
            throw new ArgumentException("Discount must be non-negative and less than unit price", nameof(discount));

        if (lineNumber <= 0)
            throw new ArgumentException("LineNumber must be greater than 0", nameof(lineNumber));

        return new QuotationLine
        {
            QuotationId = quotationId,
            PartId = partId,
            ProductVariantId = productVariantId,
            UnitId = unitId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            Discount = discount,
            LineNumber = lineNumber,
            Description = description?.Trim() ?? string.Empty
        };
    }
}
