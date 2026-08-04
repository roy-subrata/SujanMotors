namespace AutoPartShop.Domain.Enums;

/// <summary>
/// Lifecycle of a <see cref="Entities.WarrantyClaim"/>:
/// PENDING → UNDER_REVIEW → APPROVED → IN_PROGRESS → COMPLETED → CLOSED, or REJECTED.
/// Member names are serialized as-is (global JsonStringEnumConverter, no naming policy) and match
/// the historical string values stored in the database (.HasConversion&lt;string&gt;() uses
/// enum.ToString() by default) — do not rename members without a data migration.
/// </summary>
public enum WarrantyClaimStatus
{
    PENDING,
    UNDER_REVIEW,
    APPROVED,
    REJECTED,
    IN_PROGRESS,
    COMPLETED,
    CLOSED
}
