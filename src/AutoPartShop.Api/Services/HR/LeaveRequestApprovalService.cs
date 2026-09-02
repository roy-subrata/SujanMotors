using AutoPartShop.Domain.Entities.HR;
using AutoPartShop.Domain.Enums.HR;
using AutoPartShop.Domain.Repositories.HR;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Services.HR;

/// <summary>
/// Transactional implementation of leave-request approval/rejection. The repositories all share one
/// scoped <see cref="AutoPartDbContext"/>, so opening a transaction here and letting the repository
/// calls execute inside it keeps the decision and the attendance marks atomic.
/// </summary>
public class LeaveRequestApprovalService : ILeaveRequestApprovalService
{
    private readonly AutoPartDbContext _dbContext;
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAttendanceRepository _attendanceRepository;

    public LeaveRequestApprovalService(
        AutoPartDbContext dbContext,
        ILeaveRequestRepository leaveRequestRepository,
        IEmployeeRepository employeeRepository,
        IAttendanceRepository attendanceRepository)
    {
        _dbContext = dbContext;
        _leaveRequestRepository = leaveRequestRepository;
        _employeeRepository = employeeRepository;
        _attendanceRepository = attendanceRepository;
    }

    public async Task<LeaveRequest> ApproveAsync(Guid id, string notes, string currentUser, CancellationToken cancellationToken)
    {
        // The approval status AND the attendance LEAVE marks must land together: approving a leave
        // without writing the attendance marks would flip that month's payroll to count approved
        // leave days as ABSENT, so the status transition and the marks share one transaction. The
        // authoritative entitlement check runs on fresh data inside the transaction.
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                _dbContext.ChangeTracker.Clear();
                var fresh = await _leaveRequestRepository.GetByIdAsync(id, cancellationToken)
                    ?? throw new InvalidOperationException("Leave request not found");

                var employee = await _employeeRepository.GetByIdAsync(fresh.EmployeeId, cancellationToken)
                    ?? throw new InvalidOperationException("Employee not found");

                var entitlement = employee.GetLeaveEntitlement(fresh.LeaveType);
                if (entitlement.HasValue)
                {
                    var employeeRequests = await _leaveRequestRepository.GetByEmployeeAsync(fresh.EmployeeId, cancellationToken);
                    var usedDays = employeeRequests
                        .Where(r => r.Status == LeaveRequestStatus.APPROVED && r.LeaveType == fresh.LeaveType)
                        .Sum(r => r.TotalDays);
                    var remaining = entitlement.Value - usedDays;
                    if (fresh.TotalDays > remaining)
                        throw new InvalidOperationException(
                            $"Insufficient {fresh.LeaveType} leave balance. Entitlement: {entitlement} day(s), already used: {usedDays}, remaining: {Math.Max(0, remaining)}, requested: {fresh.TotalDays}.");
                }

                fresh.Approve(currentUser, notes);
                fresh.ModifiedBy = currentUser;
                await _leaveRequestRepository.UpdateAsync(fresh, cancellationToken);

                await _attendanceRepository.ApplyLeaveMarksAsync(
                    fresh.EmployeeId, fresh.FromDate, fresh.ToDate, fresh.LeaveType, currentUser, cancellationToken);

                await tx.CommitAsync(cancellationToken);
                return fresh;
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public async Task<LeaveRequest> RejectAsync(Guid id, string notes, string currentUser, CancellationToken cancellationToken)
    {
        // Reject is only legal on PENDING requests, and PENDING requests never had attendance marks
        // written, so no cleanup is required — but the decision still commits on its own so a
        // rejected request can never race an approval into a half-written state.
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                _dbContext.ChangeTracker.Clear();
                var fresh = await _leaveRequestRepository.GetByIdAsync(id, cancellationToken)
                    ?? throw new InvalidOperationException("Leave request not found");

                fresh.Reject(currentUser, notes);
                fresh.ModifiedBy = currentUser;
                await _leaveRequestRepository.UpdateAsync(fresh, cancellationToken);

                await tx.CommitAsync(cancellationToken);
                return fresh;
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }
}