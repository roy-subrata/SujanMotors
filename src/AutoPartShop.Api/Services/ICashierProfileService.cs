namespace AutoPartShop.Api.Services;

/// <summary>
/// Resolves an HR employee profile for a cashier (identified by their login user id),
/// without Core (this Api layer) needing to know that the HR module's <c>Employee</c>/
/// <c>Shift</c> entities exist. Implemented inside the HR module (Infrastructure) as the
/// sanctioned inversion for the "Core must not depend on HR" architecture boundary —
/// see <c>HrModuleBoundaryTests</c>.
/// </summary>
public interface ICashierProfileService
{
    /// <summary>
    /// Looks up the HR profile linked to the given login user id, if any. Returns null when
    /// no active employee record is linked to that user — callers should fall back to the
    /// login account's own name/username in that case.
    /// </summary>
    Task<CashierProfile?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Display-facing HR profile for a cashier. <see cref="EmployeeName"/>/<see cref="EmployeeCode"/>
/// are used to build a receipt/report display string; <see cref="ShiftLabel"/>/
/// <see cref="ShiftHours"/> are used to suggest defaults on the Open Till form. Any field may be
/// null when the employee has no shift assigned (shift fields) — <see cref="EmployeeName"/> and
/// <see cref="EmployeeCode"/> are always populated when a profile is returned at all.
/// </summary>
public sealed record CashierProfile(string? EmployeeName, string? EmployeeCode, string? ShiftLabel, string? ShiftHours);
