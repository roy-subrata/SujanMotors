namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Line item in a purchase return
/// </summary>
public class PurchaseReturnLine : AuditableEntity
{
    public Guid PurchaseReturnId { get; private set; }
    public Guid PurchaseOrderLineId { get; private set; }
    public Guid PartId { get; private set; }
    public int Quantity { get; private set; }
    public int RejectedQuantity { get; private set; } = 0;  // Quantity rejected by supplier
    public decimal UnitPrice { get; private set; }
    public decimal RefundAmount => (Quantity - RejectedQuantity) * UnitPrice;
    public string Condition { get; private set; } = string.Empty;  // UNOPENED, OPENED, DAMAGED, DEFECTIVE
    public string Notes { get; private set; } = string.Empty;

    // Navigation properties
    public PurchaseReturn? PurchaseReturn { get; set; }
    public Part? Part { get; set; }

    private PurchaseReturnLine() { }

    public static PurchaseReturnLine Create(Guid purchaseReturnId, Guid purchaseOrderLineId, Guid partId,
        int quantity, decimal unitPrice, string condition = "UNOPENED")
    {
        if (purchaseReturnId == Guid.Empty)
            throw new ArgumentException("PurchaseReturnId cannot be empty", nameof(purchaseReturnId));

        if (purchaseOrderLineId == Guid.Empty)
            throw new ArgumentException("PurchaseOrderLineId cannot be empty", nameof(purchaseOrderLineId));

        if (partId == Guid.Empty)
            throw new ArgumentException("PartId cannot be empty", nameof(partId));

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));

        if (unitPrice <= 0)
            throw new ArgumentException("UnitPrice must be greater than 0", nameof(unitPrice));

        var validConditions = new[] { "UNOPENED", "OPENED", "DAMAGED", "DEFECTIVE" };
        if (!validConditions.Contains(condition.ToUpper()))
            throw new ArgumentException($"Condition must be one of: {string.Join(", ", validConditions)}", nameof(condition));

        return new PurchaseReturnLine
        {
            PurchaseReturnId = purchaseReturnId,
            PurchaseOrderLineId = purchaseOrderLineId,
            PartId = partId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            Condition = condition.ToUpper()
        };
    }

    public void RejectQuantity(int quantity)
    {
        if (quantity < 0)
            throw new ArgumentException("Rejected quantity cannot be negative", nameof(quantity));

        if (quantity > Quantity)
            throw new InvalidOperationException("Rejected quantity cannot exceed return quantity");

        RejectedQuantity = quantity;
    }

    public void AddNotes(string notes)
    {
        Notes = notes?.Trim() ?? string.Empty;
    }
}
