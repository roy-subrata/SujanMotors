namespace AutoPartShop.Domain.Entities;

/// <summary>
/// A discount rule. Scope is determined by PartId / ProductVariantId:
///
///   PartId = null,  VariantId = null   → CART level (promo code / threshold)
///   PartId = value, VariantId = null   → Product level (all variants of that part)
///   PartId = value, VariantId = value  → Variant level (specific variant only)
///
/// Resolution at sale time (best value wins):
///   1. Variant-level discount (PartId + VariantId match)
///   2. Product-level discount (PartId match, VariantId null)
///   → Best of 1 and 2 applied to item
///   3. Cart-level discount stacked on top (promo code / threshold)
/// </summary>
public class Discount : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    // PERCENTAGE or FIXED
    public string Type { get; private set; } = "PERCENTAGE";
    public decimal Value { get; private set; }

    // Scope via nullable FK — no separate mapping tables needed
    public Guid? PartId { get; private set; }
    public Guid? ProductVariantId { get; private set; }

    // CART-level options
    public string? PromoCode { get; private set; }
    public decimal? MinimumCartAmount { get; private set; }

    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Product? Part { get; set; }
    public ProductVariant? ProductVariant { get; set; }

    private Discount() { }

    public static Discount Create(
        string name,
        string type,
        decimal value,
        DateTime startDate,
        Guid? partId = null,
        Guid? productVariantId = null,
        DateTime? endDate = null,
        string? description = null,
        string? promoCode = null,
        decimal? minimumCartAmount = null)
    {
        ValidateType(type);

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (value <= 0)
            throw new ArgumentException("Value must be greater than 0", nameof(value));

        if (type.Trim().ToUpper() == "PERCENTAGE" && value > 100)
            throw new ArgumentException("Percentage value cannot exceed 100", nameof(value));

        if (endDate.HasValue && endDate.Value.Date < startDate.Date)
            throw new ArgumentException("EndDate cannot be before StartDate", nameof(endDate));

        // ProductVariantId requires PartId
        if (productVariantId.HasValue && !partId.HasValue)
            throw new ArgumentException("PartId is required when ProductVariantId is set", nameof(partId));

        return new Discount
        {
            Name = name.Trim(),
            Description = description?.Trim(),
            Type = type.Trim().ToUpper(),
            Value = value,
            PartId = partId,
            ProductVariantId = productVariantId,
            StartDate = startDate.Date,
            EndDate = endDate?.Date,
            PromoCode = promoCode?.Trim().ToUpper(),
            MinimumCartAmount = minimumCartAmount,
            IsActive = true
        };
    }

    public void Update(
        string name,
        string type,
        decimal value,
        DateTime startDate,
        bool isActive,
        DateTime? endDate = null,
        string? description = null,
        string? promoCode = null,
        decimal? minimumCartAmount = null)
    {
        ValidateType(type);

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (value <= 0)
            throw new ArgumentException("Value must be greater than 0", nameof(value));

        if (type.Trim().ToUpper() == "PERCENTAGE" && value > 100)
            throw new ArgumentException("Percentage value cannot exceed 100", nameof(value));

        if (endDate.HasValue && endDate.Value.Date < startDate.Date)
            throw new ArgumentException("EndDate cannot be before StartDate", nameof(endDate));

        Name = name.Trim();
        Description = description?.Trim();
        Type = type.Trim().ToUpper();
        Value = value;
        StartDate = startDate.Date;
        EndDate = endDate?.Date;
        IsActive = isActive;
        PromoCode = promoCode?.Trim().ToUpper();
        MinimumCartAmount = minimumCartAmount;
    }

    public decimal CalculateDiscountAmount(decimal price)
    {
        if (price <= 0) return 0;
        return Type == "PERCENTAGE"
            ? Math.Round(price * Value / 100, 2)
            : Math.Min(Value, price);
    }

    public bool IsValidOn(DateTime date) =>
        IsActive && date.Date >= StartDate && (!EndDate.HasValue || date.Date <= EndDate.Value);

    // Scope helpers
    public bool IsCartLevel => !PartId.HasValue && !ProductVariantId.HasValue;
    public bool IsProductLevel => PartId.HasValue && !ProductVariantId.HasValue;
    public bool IsVariantLevel => PartId.HasValue && ProductVariantId.HasValue;

    public string Scope => IsVariantLevel ? "VARIANT" : IsProductLevel ? "PRODUCT" : "CART";

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    private static void ValidateType(string type)
    {
        var valid = new[] { "PERCENTAGE", "FIXED" };
        if (!valid.Contains(type?.Trim().ToUpper()))
            throw new ArgumentException("Type must be PERCENTAGE or FIXED", nameof(type));
    }
}
