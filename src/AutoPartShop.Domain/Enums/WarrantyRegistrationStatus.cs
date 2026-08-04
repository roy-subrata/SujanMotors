namespace AutoPartShop.Domain.Enums;

/// <summary>
/// Lifecycle of a <see cref="Entities.WarrantyRegistration"/>: ACTIVE → EXPIRED, ACTIVE → CLAIMED
/// (reactivates back to ACTIVE/EXPIRED after the claim resolves), or ACTIVE → VOID.
/// Member names are serialized as-is (global JsonStringEnumConverter, no naming policy) and match
/// the historical string values stored in the database (.HasConversion&lt;string&gt;() uses
/// enum.ToString() by default) — do not rename members without a data migration.
/// </summary>
public enum WarrantyRegistrationStatus
{
    ACTIVE,
    EXPIRED,
    CLAIMED,
    VOID
}
