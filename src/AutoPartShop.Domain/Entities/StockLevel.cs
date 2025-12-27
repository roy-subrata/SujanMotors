namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Tracks the current stock level of a part in a specific warehouse
/// </summary>
public class StockLevel : AuditableEntity
{
    public Guid PartId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public int QuantityOnHand { get; private set; } = 0;  // Current stock
    public int QuantityReserved { get; private set; } = 0;  // Reserved for orders
    public int QuantityAvailable => QuantityOnHand - QuantityReserved;  // Available for sale
    public int ReorderLevel { get; private set; } = 0;  // Minimum stock level
    public int ReorderQuantity { get; private set; } = 0;  // Quantity to order when below reorder level
    public string Location { get; private set; } = string.Empty;  // Shelf/bin location in warehouse
    public bool IsActive { get; private set; } = true;

    // Navigation properties
    public Part? Part { get; set; }
    public Warehouse? Warehouse { get; set; }
    public ICollection<StockMovement> Movements { get; set; } = new List<StockMovement>();

    private StockLevel() { }

    public static StockLevel Create(Guid partId, Guid warehouseId, int reorderLevel = 0, int reorderQuantity = 0, string location = "")
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
            ReorderLevel = reorderLevel,
            ReorderQuantity = reorderQuantity,
            Location = location?.Trim() ?? string.Empty,
            IsActive = true
        };
    }

    public void AddStock(int quantity, string reason = "")
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));

        QuantityOnHand += quantity;
    }

    public void RemoveStock(int quantity, string reason = "")
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));

        if (quantity > QuantityAvailable)
            throw new InvalidOperationException("Insufficient stock available");

        QuantityOnHand -= quantity;
    }

    public void ReserveStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));

        if (quantity > QuantityAvailable)
            throw new InvalidOperationException("Insufficient stock available to reserve");

        QuantityReserved += quantity;
    }

    public void ReleaseReservedStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));

        if (quantity > QuantityReserved)
            throw new InvalidOperationException("Cannot release more than reserved");

        QuantityReserved -= quantity;
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
