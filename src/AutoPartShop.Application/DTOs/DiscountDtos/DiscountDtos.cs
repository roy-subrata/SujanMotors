namespace AutoPartShop.Application.DTOs.DiscountDtos;

public class DiscountResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;        // PERCENTAGE | FIXED
    public decimal Value { get; set; }
    public string Scope { get; set; } = string.Empty;       // VARIANT | PRODUCT | CART (computed)
    public Guid? PartId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public string? PromoCode { get; set; }
    public decimal? MinimumCartAmount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

public class CreateDiscountRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>PERCENTAGE or FIXED</summary>
    public string Type { get; set; } = "PERCENTAGE";

    public decimal Value { get; set; }

    /// <summary>
    /// null + null  → CART level
    /// PartId only  → Product level
    /// Both set     → Variant level
    /// </summary>
    public Guid? PartId { get; set; }
    public Guid? ProductVariantId { get; set; }

    public string? PromoCode { get; set; }
    public decimal? MinimumCartAmount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class UpdateDiscountRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = "PERCENTAGE";
    public decimal Value { get; set; }
    public string? PromoCode { get; set; }
    public decimal? MinimumCartAmount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Result returned after discount resolution for a line item</summary>
public class DiscountResolutionResult
{
    public Guid? DiscountId { get; set; }
    public string? DiscountName { get; set; }
    public string? DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal DiscountAmount { get; set; }
    public string AppliedLevel { get; set; } = "NONE"; // VARIANT | PRODUCT | CART | NONE
    public decimal FinalPrice { get; set; }
}
