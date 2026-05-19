namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Represents a physical location where a product is stored within a warehouse
/// </summary>
public class ProductLocation : AuditableEntity
{
    public Guid PartId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public string Section { get; private set; } = string.Empty;
    public string Shelf { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public bool IsPrimary { get; private set; }

    // Navigation properties
    public virtual Part Part { get; set; } = null!;
    public virtual Warehouse Warehouse { get; set; } = null!;

    private ProductLocation() { }

    public static ProductLocation Create(
        Guid partId,
        Guid warehouseId,
        string section,
        string shelf,
        bool isPrimary = false,
        string? notes = null)
    {
        if (partId == Guid.Empty)
            throw new ArgumentException("Part ID is required", nameof(partId));
        if (warehouseId == Guid.Empty)
            throw new ArgumentException("Warehouse ID is required", nameof(warehouseId));
        if (string.IsNullOrWhiteSpace(section))
            throw new ArgumentException("Section/Aisle is required", nameof(section));
        if (string.IsNullOrWhiteSpace(shelf))
            throw new ArgumentException("Shelf/Bin is required", nameof(shelf));

        return new ProductLocation
        {
            PartId = partId,
            WarehouseId = warehouseId,
            Section = section.Trim(),
            Shelf = shelf.Trim(),
            IsPrimary = isPrimary,
            Notes = notes?.Trim(),
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            Isdeleted = false
        };
    }

    public void Update(string section, string shelf, bool isPrimary, string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(section))
            throw new ArgumentException("Section/Aisle is required", nameof(section));
        if (string.IsNullOrWhiteSpace(shelf))
            throw new ArgumentException("Shelf/Bin is required", nameof(shelf));

        Section = section.Trim();
        Shelf = shelf.Trim();
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

    public string GetFullLocation()
    {
        return $"{Section} / {Shelf}";
    }
}
