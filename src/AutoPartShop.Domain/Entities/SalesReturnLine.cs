namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Line item in a sales return
/// </summary>
public class SalesReturnLine : AuditableEntity
{
    public Guid SalesReturnId { get; private set; }
    public Guid SalesOrderLineId { get; private set; }
    public Guid PartId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal RefundAmount => Quantity * UnitPrice;
    public string Condition { get; private set; } = string.Empty;  // UNOPENED, OPENED, DAMAGED
    public string Notes { get; private set; } = string.Empty;

    // Navigation properties
    public SalesReturn? SalesReturn { get; set; }
    public Part? Part { get; set; }

    private SalesReturnLine() { }

    public static SalesReturnLine Create(Guid salesReturnId, Guid salesOrderLineId, Guid partId,
        int quantity, decimal unitPrice, string condition = "UNOPENED")
    {
        if (salesReturnId == Guid.Empty)
            throw new ArgumentException("SalesReturnId cannot be empty", nameof(salesReturnId));

        if (salesOrderLineId == Guid.Empty)
            throw new ArgumentException("SalesOrderLineId cannot be empty", nameof(salesOrderLineId));

        if (partId == Guid.Empty)
            throw new ArgumentException("PartId cannot be empty", nameof(partId));

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));

        if (unitPrice <= 0)
            throw new ArgumentException("UnitPrice must be greater than 0", nameof(unitPrice));

        var validConditions = new[] { "UNOPENED", "OPENED", "DAMAGED" };
        if (!validConditions.Contains(condition.ToUpper()))
            throw new ArgumentException($"Condition must be one of: {string.Join(", ", validConditions)}", nameof(condition));

        return new SalesReturnLine
        {
            SalesReturnId = salesReturnId,
            SalesOrderLineId = salesOrderLineId,
            PartId = partId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            Condition = condition.ToUpper()
        };
    }

    public void AddNotes(string notes)
    {
        Notes = notes?.Trim() ?? string.Empty;
    }
}
