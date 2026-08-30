using AutoPartShop.Domain.Entities.HR;

namespace AutoPartShop.Domain.Repositories.HR;

public interface ILeaveRequestRepository
{
    Task<LeaveRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(LeaveRequest entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(LeaveRequest entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// True when the employee already has an APPROVED or PENDING request overlapping the range.
    /// </summary>
    Task<bool> HasOverlapAsync(Guid employeeId, DateTime fromDate, DateTime toDate, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// All non-deleted leave requests for an employee (any status). Used to compute
    /// leave balance (approved usage) and to enforce entitlements at approval time.
    /// </summary>
    Task<IReadOnlyList<LeaveRequest>> GetByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);
}
