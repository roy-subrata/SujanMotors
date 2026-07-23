namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Join between a Product and a physical <see cref="WarehouseLocation"/> bin/shelf — "this part
/// lives at that bin". A part can have several locations (IsPrimary marks the main one); a bin can
/// hold several different parts. The location itself (Zone/Aisle/Rack/Bin) is owned by
/// <see cref="WarehouseLocation"/>, not this entity — this used to carry free-text Section/Shelf
/// directly, but that let staff type anything with no link to a real, printable, structured bin.
/// </summary>
public class ProductLocation : AuditableEntity
{
    public Guid PartId { get; private set; }
    public Guid WarehouseLocationId { get; private set; }
    public string? Notes { get; private set; }
    public bool IsPrimary { get; private set; }

    // Navigation properties
    public virtual Product Part { get; set; } = null!;
    public virtual WarehouseLocation Location { get; set; } = null!;

    private ProductLocation() { }

    public static ProductLocation Create(
        Guid partId,
        Guid warehouseLocationId,
        bool isPrimary = false,
        string? notes = null)
    {
        if (partId == Guid.Empty)
            throw new ArgumentException("Part ID is required", nameof(partId));
        if (warehouseLocationId == Guid.Empty)
            throw new ArgumentException("Warehouse location ID is required", nameof(warehouseLocationId));

        return new ProductLocation
        {
            PartId = partId,
            WarehouseLocationId = warehouseLocationId,
            IsPrimary = isPrimary,
            Notes = notes?.Trim(),
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            Isdeleted = false
        };
    }

    public void Update(Guid warehouseLocationId, bool isPrimary, string? notes = null)
    {
        if (warehouseLocationId == Guid.Empty)
            throw new ArgumentException("Warehouse location ID is required", nameof(warehouseLocationId));

        WarehouseLocationId = warehouseLocationId;
        IsPrimary = isPrimary;
        Notes = notes?.Trim();
        ModifiedDate = DateTime.UtcNow;
    }

    public void SetAsPrimary()
    {
        IsPrimary = true;
        ModifiedDate = DateTime.UtcNow;
    }

    public void UnsetPrimary()
    {
        IsPrimary = false;
        ModifiedDate = DateTime.UtcNow;
    }
}
