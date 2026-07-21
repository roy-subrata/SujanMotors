namespace AutoPartShop.Domain.Entities;

/// <summary>
/// A physical bin/shelf slot inside a warehouse (Zone-Aisle-Rack-Bin), independent of any
/// specific product. Exists so a location label can be printed and stuck on an empty shelf
/// before any stock is assigned there. Not to be confused with <see cref="ProductLocation"/>,
/// which is a per-product "this part lives at Section/Shelf" mapping.
/// </summary>
public class WarehouseLocation : AuditableEntity
{
    public Guid WarehouseId { get; private set; }
    public string Zone { get; private set; } = string.Empty;
    public string Aisle { get; private set; } = string.Empty;
    public string Rack { get; private set; } = string.Empty;
    public string Bin { get; private set; } = string.Empty;

    /// <summary>
    /// Optional "what's mostly stored here" hint (e.g. BRAKES &amp; SUSPENSION) shown on the
    /// printed label. Informational only — does not constrain which products can be stocked here.
    /// </summary>
    public Guid? CategoryId { get; private set; }

    public string? Notes { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navigation properties
    public virtual Warehouse Warehouse { get; set; } = null!;
    public virtual Category? Category { get; set; }

    private WarehouseLocation() { }

    public static WarehouseLocation Create(
        Guid warehouseId,
        string zone,
        string aisle,
        string rack,
        string bin,
        Guid? categoryId = null,
        string? notes = null)
    {
        if (warehouseId == Guid.Empty)
            throw new ArgumentException("Warehouse ID is required", nameof(warehouseId));
        if (string.IsNullOrWhiteSpace(zone))
            throw new ArgumentException("Zone is required", nameof(zone));
        if (zone.Trim().Length > 10)
            throw new ArgumentException("Zone cannot exceed 10 characters", nameof(zone));
        if (string.IsNullOrWhiteSpace(aisle))
            throw new ArgumentException("Aisle is required", nameof(aisle));
        if (aisle.Trim().Length > 10)
            throw new ArgumentException("Aisle cannot exceed 10 characters", nameof(aisle));
        if (string.IsNullOrWhiteSpace(rack))
            throw new ArgumentException("Rack is required", nameof(rack));
        if (rack.Trim().Length > 10)
            throw new ArgumentException("Rack cannot exceed 10 characters", nameof(rack));
        if (string.IsNullOrWhiteSpace(bin))
            throw new ArgumentException("Bin is required", nameof(bin));
        if (bin.Trim().Length > 10)
            throw new ArgumentException("Bin cannot exceed 10 characters", nameof(bin));

        return new WarehouseLocation
        {
            WarehouseId = warehouseId,
            Zone = zone.Trim(),
            Aisle = aisle.Trim(),
            Rack = rack.Trim(),
            Bin = bin.Trim(),
            CategoryId = categoryId,
            Notes = notes?.Trim(),
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            Isdeleted = false
        };
    }

    public void Update(
        Guid warehouseId,
        string zone,
        string aisle,
        string rack,
        string bin,
        Guid? categoryId = null,
        string? notes = null)
    {
        if (warehouseId == Guid.Empty)
            throw new ArgumentException("Warehouse ID is required", nameof(warehouseId));
        if (string.IsNullOrWhiteSpace(zone))
            throw new ArgumentException("Zone is required", nameof(zone));
        if (zone.Trim().Length > 10)
            throw new ArgumentException("Zone cannot exceed 10 characters", nameof(zone));
        if (string.IsNullOrWhiteSpace(aisle))
            throw new ArgumentException("Aisle is required", nameof(aisle));
        if (aisle.Trim().Length > 10)
            throw new ArgumentException("Aisle cannot exceed 10 characters", nameof(aisle));
        if (string.IsNullOrWhiteSpace(rack))
            throw new ArgumentException("Rack is required", nameof(rack));
        if (rack.Trim().Length > 10)
            throw new ArgumentException("Rack cannot exceed 10 characters", nameof(rack));
        if (string.IsNullOrWhiteSpace(bin))
            throw new ArgumentException("Bin is required", nameof(bin));
        if (bin.Trim().Length > 10)
            throw new ArgumentException("Bin cannot exceed 10 characters", nameof(bin));

        WarehouseId = warehouseId;
        Zone = zone.Trim();
        Aisle = aisle.Trim();
        Rack = rack.Trim();
        Bin = bin.Trim();
        CategoryId = categoryId;
        Notes = notes?.Trim();
        ModifiedDate = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        ModifiedDate = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Computed, not stored — e.g. "A-04-B-12". Matches the barcode label format.
    /// </summary>
    public string GetLocationCode() => $"{Zone}-{Aisle}-{Rack}-{Bin}";
}
