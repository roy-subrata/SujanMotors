using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Domain.Repositories;

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
}
