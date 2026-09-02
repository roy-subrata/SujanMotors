using AutoPartShop.Domain.Entities.HR;

namespace AutoPartShop.Api.Services.HR;

/// <summary>
/// Owns the transactional approve/reject flow for leave requests. Approving a leave must commit the
/// status transition AND the attendance LEAVE marks together (otherwise that month's payroll counts
/// approved leave days as ABSENT), and both must race-check entitlement on fresh data. This is EF
/// plumbing the controller should not touch, so it lives behind a service.
/// </summary>
public interface ILeaveRequestApprovalService
{
    /// <summary>Approves the request within a transaction, writing attendance LEAVE marks.</summary>
    /// <returns>The approved request.</returns>
    /// <exception cref="InvalidOperationException">When the request is gone, already decided, or the
    /// employee has insufficient remaining leave balance.</exception>
    Task<LeaveRequest> ApproveAsync(Guid id, string notes, string currentUser, CancellationToken cancellationToken);

    /// <summary>Rejects the request within a transaction. Legal only while it is PENDING.</summary>
    /// <returns>The rejected request.</returns>
    /// <exception cref="InvalidOperationException">When the request is gone or already decided.</exception>
    Task<LeaveRequest> RejectAsync(Guid id, string notes, string currentUser, CancellationToken cancellationToken);
}