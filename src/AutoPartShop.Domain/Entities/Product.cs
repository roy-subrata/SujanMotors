using AutoPartsShop.Domain.Entities;

namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Represents an auto part/product in the inventory system
/// </summary>
public class Part : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public PartNumber PartNumber { get; private set; } = null!;
    public string SKU { get; private set; } = string.Empty;  // Stock Keeping Unit
    public Guid CategoryId { get; private set; }
    public Guid? BrandId { get; private set; }  // Optional reference to Brand
    public Guid? UnitId { get; private set; }  // Optional reference to Unit for measuring/stocking
    public decimal CostPrice { get; private set; } = 0;  // Purchase price
    public decimal SellingPrice { get; private set; } = 0;  // Retail/Sale price
    public int MinimumStock { get; private set; } = 0;  // Reorder level
    public bool IsActive { get; private set; } = true;

    // Navigation properties
    public Category? Category { get; set; }
    public Brand? Brand { get; set; }
    public Unit? Unit { get; set; }
    public ICollection<PartVehicleCompatibility> VehicleCompatibilities { get; set; } = new List<PartVehicleCompatibility>();

    private Part() { }

    public static Part Create(string name, PartNumber partNumber, string sku, Guid categoryId, Guid? brandId = null, Guid? unitId = null, string description = "")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (partNumber is null)
            throw new InvalidOperationException($"PartNumber cannot be empty {nameof(partNumber)}");

        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU cannot be empty", nameof(sku));

        if (categoryId == Guid.Empty)
            throw new ArgumentException("CategoryId cannot be empty", nameof(categoryId));

        if (name.Length > 200)
            throw new ArgumentException("Name cannot exceed 200 characters", nameof(name));

        if (sku.Length > 50)
            throw new ArgumentException("SKU cannot exceed 50 characters", nameof(sku));

        return new Part
        {
            Name = name.Trim(),
            Description = description?.Trim() ?? string.Empty,
            PartNumber = partNumber,
            SKU = sku.Trim().ToUpper(),
            CategoryId = categoryId,
            BrandId = brandId,
            UnitId = unitId,
            IsActive = true
        };
    }

    public void Update(string name, string description, string sku, Guid categoryId, Guid? brandId, Guid? unitId,
        decimal costPrice, decimal sellingPrice, int minimumStock, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU cannot be empty", nameof(sku));

        if (categoryId == Guid.Empty)
            throw new ArgumentException("CategoryId cannot be empty", nameof(categoryId));

        if (costPrice < 0)
            throw new ArgumentException("Cost price cannot be negative", nameof(costPrice));

        if (sellingPrice < 0)
            throw new ArgumentException("Selling price cannot be negative", nameof(sellingPrice));

        if (minimumStock < 0)
            throw new ArgumentException("Minimum stock cannot be negative", nameof(minimumStock));

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        SKU = sku.Trim().ToUpper();
        CategoryId = categoryId;
        BrandId = brandId;
        UnitId = unitId;
        CostPrice = costPrice;
        SellingPrice = sellingPrice;
        MinimumStock = minimumStock;
        IsActive = isActive;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}