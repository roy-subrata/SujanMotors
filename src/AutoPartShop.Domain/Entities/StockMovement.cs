namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Records a stock movement (in or out) for audit trail
/// </summary>
public class StockMovement : AuditableEntity
{
    public Guid StockLevelId { get; private set; }
    public Guid? PurchaseOrderLineId { get; private set; }  // If from PO receipt
    public Guid? SalesOrderLineId { get; private set; }  // If from SO dispatch
    public Guid? UnitId { get; private set; }  // Unit in which the movement was recorded
    public string MovementType { get; private set; } = string.Empty;  // IN, OUT, RETURN, ADJUST, TRANSFER
    public int Quantity { get; private set; }
    public int QuantityInBaseUnit { get; private set; }  // Quantity converted to base unit
    public string Reason { get; private set; } = string.Empty;  // GRN, Dispatch, Damage, Loss, etc.
    public string ReferenceNumber { get; private set; } = string.Empty;  // PO, SO, Invoice, etc.
    public DateTime MovementDate { get; private set; }
    public string ApprovedBy { get; private set; } = string.Empty;
    public string Notes { get; private set; } = string.Empty;

    // Navigation properties
    public StockLevel? StockLevel { get; set; }
    public Unit? Unit { get; set; }

    private StockMovement() { }

    public static StockMovement Create(Guid stockLevelId, string movementType, int quantity,
        string reason = "", string referenceNumber = "", DateTime? movementDate = null,
        Guid? unitId = null, int quantityInBaseUnit = 0)
    {
        if (stockLevelId == Guid.Empty)
            throw new ArgumentException("StockLevelId cannot be empty", nameof(stockLevelId));

        if (string.IsNullOrWhiteSpace(movementType))
            throw new ArgumentException("MovementType cannot be empty", nameof(movementType));

        if (quantity == 0)
            throw new ArgumentException("Quantity cannot be zero", nameof(quantity));

        var validTypes = new[] { "IN", "OUT", "RETURN", "ADJUST", "TRANSFER" };
        if (!validTypes.Contains(movementType.ToUpper()))
            throw new ArgumentException($"MovementType must be one of: {string.Join(", ", validTypes)}", nameof(movementType));

        return new StockMovement
        {
            StockLevelId = stockLevelId,
            MovementType = movementType.ToUpper(),
            Quantity = quantity,
            QuantityInBaseUnit = quantityInBaseUnit > 0 ? quantityInBaseUnit : quantity,
            Reason = reason?.Trim() ?? string.Empty,
            ReferenceNumber = referenceNumber?.Trim() ?? string.Empty,
            MovementDate = movementDate ?? DateTime.UtcNow,
            UnitId = unitId,
            Notes = string.Empty
        };
    }

    public void Approve(string approvedBy)
    {
        if (string.IsNullOrWhiteSpace(approvedBy))
            throw new ArgumentException("ApprovedBy cannot be empty", nameof(approvedBy));

        ApprovedBy = approvedBy.Trim();
    }

    public void AddNotes(string notes)
    {
        Notes = notes?.Trim() ?? string.Empty;
    }
}
