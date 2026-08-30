namespace AutoPartShop.Domain.Enums;

/// <summary>
/// Lifecycle of a <see cref="Entities.StockTake"/>: COUNTING → REVIEW → COMPLETED, or CANCELLED
/// (from COUNTING/REVIEW).
/// Member names are serialized as-is (global JsonStringEnumConverter, no naming policy) and match
/// the historical string values stored in the database (.HasConversion&lt;string&gt;() uses
/// enum.ToString() by default) — do not rename members without a data migration.
/// </summary>
public enum StockTakeStatus
{
    COUNTING,
    REVIEW,
    COMPLETED,
    CANCELLED
}
