namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Tracks movements of stock from a specific lot
/// Records FIFO/LIFO consumption for accurate inventory costing
/// </summary>
public class StockLotMovement : AuditableEntity
{
    public Guid StockLotId { get; private set; }
    public Guid? UnitId { get; private set; }  // Unit in which the movement was recorded
    public int Quantity { get; private set; } = 0;
    public int QuantityInBaseUnit { get; private set; } = 0;  // Quantity in base unit
    public string MovementType { get; private set; } = string.Empty;  // RECEIPT, SALE, ADJUSTMENT, DAMAGE, RETURN, TRANSFER
    public Guid? ReferenceId { get; private set; }  // SalesOrderLineId, SalesReturnLineId, StockMovementId, etc.
    public string ReferenceType { get; private set; } = string.Empty;  // SalesOrderLine, SalesReturnLine, etc.
    public DateTime MovementDate { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public decimal CostAtMovement { get; private set; } = 0;  // Cost price at time of movement
    public decimal CostAtMovementInBaseUnit { get; private set; } = 0;  // Cost in base unit
    public string Notes { get; private set; } = string.Empty;

    // Navigation properties
    public StockLot? StockLot { get; set; }
    public Unit? Unit { get; set; }

    private StockLotMovement() { }

    public static StockLotMovement Create(Guid stockLotId, int quantity, string movementType,
        Guid? referenceId = null, string referenceType = "", DateTime? movementDate = null,
        decimal costAtMovement = 0, string reason = "", string notes = "", Guid? unitId = null,
        int quantityInBaseUnit = 0, decimal costAtMovementInBaseUnit = 0)
    {
        if (stockLotId == Guid.Empty)
            throw new ArgumentException("StockLotId cannot be empty", nameof(stockLotId));

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));

        if (string.IsNullOrWhiteSpace(movementType))
            throw new ArgumentException("MovementType cannot be empty", nameof(movementType));

        var validMovementTypes = new[] { "RECEIPT", "SALE", "ADJUSTMENT", "DAMAGE", "RETURN", "TRANSFER" };
        if (!validMovementTypes.Contains(movementType.ToUpper()))
            throw new ArgumentException($"MovementType must be one of: {string.Join(", ", validMovementTypes)}", nameof(movementType));

        if (costAtMovement < 0)
            throw new ArgumentException("CostAtMovement cannot be negative", nameof(costAtMovement));

        return new StockLotMovement
        {
            StockLotId = stockLotId,
            Quantity = quantity,
            QuantityInBaseUnit = quantityInBaseUnit > 0 ? quantityInBaseUnit : quantity,
            MovementType = movementType.Trim().ToUpper(),
            ReferenceId = referenceId,
            ReferenceType = referenceType?.Trim() ?? string.Empty,
            MovementDate = movementDate ?? DateTime.UtcNow,
            CostAtMovement = costAtMovement,
            CostAtMovementInBaseUnit = costAtMovementInBaseUnit > 0 ? costAtMovementInBaseUnit : costAtMovement,
            Reason = reason?.Trim() ?? string.Empty,
            Notes = notes?.Trim() ?? string.Empty,
            UnitId = unitId
        };
    }

    public decimal GetMovementCost() => Quantity * CostAtMovement;
    public decimal GetMovementCostInBaseUnit() => QuantityInBaseUnit * CostAtMovementInBaseUnit;

    public void UpdateNotes(string notes)
    {
        Notes = notes?.Trim() ?? string.Empty;
    }
}
