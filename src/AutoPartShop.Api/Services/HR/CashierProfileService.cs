using AutoPartShop.Domain.Repositories.HR;

namespace AutoPartShop.Api.Services.HR;

/// <summary>
/// The HR-side adapter for <see cref="ICashierProfileService"/> — the sanctioned inversion for
/// the "Core must not depend on HR" module boundary (see <c>HrModuleBoundaryTests</c>). Core
/// (e.g. <c>TillSessionController</c>) depends only on the interface; this implementation is the
/// only piece of Core-facing code that is allowed to know <c>Employee</c>/<c>Shift</c> exist, and
/// it resolves them via HR's own repository abstractions rather than a raw DbContext query.
/// </summary>
/// <remarks>
/// This lives in the Api project (not Infrastructure) because <see cref="ICashierProfileService"/>
/// is defined in the Api project and Infrastructure has no project reference back to Api (Api →
/// Infrastructure only) — implementing it in Infrastructure would require a circular project
/// reference. Namespacing it under <c>.Services.HR</c>, alongside <c>Api/Controllers/HR</c>,
/// keeps it grouped with the rest of the HR module's Api-layer footprint.
/// </remarks>
public sealed class CashierProfileService(
    IEmployeeRepository employeeRepository,
    IShiftRepository shiftRepository) : ICashierProfileService
{
    public async Task<CashierProfile?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var employee = await employeeRepository.GetByUserIdAsync(userId, cancellationToken);
        if (employee is null)
            return null;

        string? shiftLabel = null;
        string? shiftHours = null;
        if (employee.ShiftId is Guid shiftId)
        {
            var shift = await shiftRepository.GetByIdAsync(shiftId, cancellationToken);
            if (shift is not null)
            {
                shiftLabel = shift.Name;
                shiftHours = $"{shift.StartTime:hh\\:mm} – {shift.EndTime:hh\\:mm}";
            }
        }

        return new CashierProfile(employee.Name, employee.EmployeeCode, shiftLabel, shiftHours);
    }
}
