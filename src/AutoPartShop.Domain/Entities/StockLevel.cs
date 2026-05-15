namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Tracks the current stock level of a part in a specific warehouse
/// </summary>
public class StockLevel : AuditableEntity
{
    public Guid PartId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid? UnitId { get; private set; }  // Unit in which stock quantities are measured
    public int QuantityOnHand { get; private set; } = 0;  // Current stock
    public int QuantityOnHandInBaseUnit { get; private set; } = 0;  // Stock in base unit
    public int QuantityReserved { get; private set; } = 0;  // Reserved for orders
    public int QuantityReservedInBaseUnit { get; private set; } = 0;  // Reserved in base unit
    public int QuantityAvailable => QuantityOnHand - QuantityReserved;  // Available for sale
    public int QuantityAvailableInBaseUnit => QuantityOnHandInBaseUnit - QuantityReservedInBaseUnit;
    public int ReorderLevel { get; private set; } = 0;  // Minimum stock level
    public int ReorderQuantity { get; private set; } = 0;  // Quantity to order when below reorder level
    public string Location { get; private set; } = string.Empty;  // Shelf/bin location in warehouse
    public bool IsActive { get; private set; } = true;

    // Navigation properties
    public Part? Part { get; set; }
    public Warehouse? Warehouse { get; set; }
    public Unit? Unit { get; set; }
    public ICollection<StockMovement> Movements { get; set; } = new List<StockMovement>();

    private StockLevel() { }

    public static StockLevel Create(Guid partId, Guid warehouseId, int reorderLevel = 0, 
        int reorderQuantity = 0, string location = "", Guid? unitId = null)
    {
        if (partId == Guid.Empty)
            throw new ArgumentException("PartId cannot be empty", nameof(partId));

        if (warehouseId == Guid.Empty)
            throw new ArgumentException("WarehouseId cannot be empty", nameof(warehouseId));

        if (reorderLevel < 0)
            throw new ArgumentException("Reorder level cannot be negative", nameof(reorderLevel));

        if (reorderQuantity < 0)
            throw new ArgumentException("Reorder quantity cannot be negative", nameof(reorderQuantity));

        return new StockLevel
        {
            PartId = partId,
            WarehouseId = warehouseId,
            UnitId = unitId,
            ReorderLevel = reorderLevel,
            ReorderQuantity = reorderQuantity,
            Location = location?.Trim() ?? string.Empty,
            IsActive = true
        };
    }

    public void AddStock(int quantity, int quantityInBaseUnit = 0, string reason = "")
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));

        QuantityOnHand += quantity;
        QuantityOnHandInBaseUnit += quantityInBaseUnit > 0 ? quantityInBaseUnit : quantity;
    }

    public void RemoveStock(int quantity, int quantityInBaseUnit = 0, string reason = "")
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));

        if (quantity > QuantityAvailable)
            throw new InvalidOperationException("Insufficient stock available");

        var baseUnitToRemove = quantityInBaseUnit > 0 ? quantityInBaseUnit : quantity;
        if (baseUnitToRemove > QuantityOnHandInBaseUnit)
            throw new InvalidOperationException("Insufficient base unit stock available");

        QuantityOnHand -= quantity;
        QuantityOnHandInBaseUnit -= baseUnitToRemove;
    }

    public void ReserveStock(int quantity, int quantityInBaseUnit = 0)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));

        if (quantity > QuantityAvailable)
            throw new InvalidOperationException("Insufficient stock available to reserve");

        QuantityReserved += quantity;
        QuantityReservedInBaseUnit += quantityInBaseUnit > 0 ? quantityInBaseUnit : quantity;
    }

    public void ReleaseReservedStock(int quantity, int quantityInBaseUnit = 0)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));

        if (quantity > QuantityReserved)
            throw new InvalidOperationException("Cannot release more than reserved");

        QuantityReserved -= quantity;
        QuantityReservedInBaseUnit -= quantityInBaseUnit > 0 ? quantityInBaseUnit : quantity;
    }

    public void UpdateReorderLevel(int reorderLevel, int reorderQuantity)
    {
        if (reorderLevel < 0)
            throw new ArgumentException("Reorder level cannot be negative", nameof(reorderLevel));

        if (reorderQuantity < 0)
            throw new ArgumentException("Reorder quantity cannot be negative", nameof(reorderQuantity));

        ReorderLevel = reorderLevel;
        ReorderQuantity = reorderQuantity;
    }

    public bool NeedsReorder => QuantityOnHand <= ReorderLevel;

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
