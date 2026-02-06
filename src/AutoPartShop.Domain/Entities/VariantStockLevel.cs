namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Tracks the stock level of a product variant in a specific warehouse.
/// </summary>
public class VariantStockLevel : AuditableEntity
{
    public Guid VariantId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public int QuantityOnHand { get; private set; } = 0;
    public int QuantityReserved { get; private set; } = 0;
    public int QuantityAvailable => QuantityOnHand - QuantityReserved;
    public int ReorderLevel { get; private set; } = 0;
    public int ReorderQuantity { get; private set; } = 0;
    public string Location { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    public ProductVariant? Variant { get; set; }
    public Warehouse? Warehouse { get; set; }

    private VariantStockLevel() { }

    public static VariantStockLevel Create(
        Guid variantId,
        Guid warehouseId,
        int reorderLevel = 0,
        int reorderQuantity = 0,
        string location = "")
    {
        if (variantId == Guid.Empty)
            throw new ArgumentException("VariantId cannot be empty", nameof(variantId));

        if (warehouseId == Guid.Empty)
            throw new ArgumentException("WarehouseId cannot be empty", nameof(warehouseId));

        if (reorderLevel < 0)
            throw new ArgumentException("Reorder level cannot be negative", nameof(reorderLevel));

        if (reorderQuantity < 0)
            throw new ArgumentException("Reorder quantity cannot be negative", nameof(reorderQuantity));

        return new VariantStockLevel
        {
            VariantId = variantId,
            WarehouseId = warehouseId,
            ReorderLevel = reorderLevel,
            ReorderQuantity = reorderQuantity,
            Location = location?.Trim() ?? string.Empty,
            IsActive = true
        };
    }

    public void AddStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));

        QuantityOnHand += quantity;
    }

    public void RemoveStock(int quantity)
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

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
