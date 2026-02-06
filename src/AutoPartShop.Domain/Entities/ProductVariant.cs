namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Sellable variant of a part for e-commerce.
/// </summary>
public class ProductVariant : AuditableEntity
{
    public Guid PartId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string? SKU { get; private set; }
    public decimal? CostPrice { get; private set; }
    public decimal? SellingPrice { get; private set; }
    public string Currency { get; private set; } = "BDT";
    public bool IsActive { get; private set; } = true;

    public Part? Part { get; set; }
    public ICollection<VariantAttributeValue> Attributes { get; set; } = new List<VariantAttributeValue>();
    public ICollection<ProductMedia> Media { get; set; } = new List<ProductMedia>();
    public ICollection<VariantStockLevel> StockLevels { get; set; } = new List<VariantStockLevel>();

    private ProductVariant() { }

    public static ProductVariant Create(
        Guid partId,
        string name,
        string code,
        string? sku = null,
        decimal? costPrice = null,
        decimal? sellingPrice = null,
        string currency = "BDT",
        bool isActive = true)
    {
        if (partId == Guid.Empty)
            throw new ArgumentException("PartId cannot be empty", nameof(partId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code cannot be empty", nameof(code));

        if (costPrice.HasValue && costPrice.Value < 0)
            throw new ArgumentException("Cost price cannot be negative", nameof(costPrice));

        if (sellingPrice.HasValue && sellingPrice.Value < 0)
            throw new ArgumentException("Selling price cannot be negative", nameof(sellingPrice));

        return new ProductVariant
        {
            PartId = partId,
            Name = name.Trim(),
            Code = code.Trim().ToUpperInvariant(),
            SKU = sku?.Trim().ToUpperInvariant(),
            CostPrice = costPrice,
            SellingPrice = sellingPrice,
            Currency = string.IsNullOrWhiteSpace(currency) ? "BDT" : currency.Trim().ToUpperInvariant(),
            IsActive = isActive
        };
    }

    public void Update(
        string name,
        string code,
        string? sku,
        decimal? costPrice,
        decimal? sellingPrice,
        string currency,
        bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code cannot be empty", nameof(code));

        if (costPrice.HasValue && costPrice.Value < 0)
            throw new ArgumentException("Cost price cannot be negative", nameof(costPrice));

        if (sellingPrice.HasValue && sellingPrice.Value < 0)
            throw new ArgumentException("Selling price cannot be negative", nameof(sellingPrice));

        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
        SKU = sku?.Trim().ToUpperInvariant();
        CostPrice = costPrice;
        SellingPrice = sellingPrice;
        Currency = string.IsNullOrWhiteSpace(currency) ? "BDT" : currency.Trim().ToUpperInvariant();
        IsActive = isActive;
    }
}
