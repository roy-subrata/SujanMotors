using AutoPartShop.Domain.Entities.HR;

namespace AutoPartShop.Domain.Repositories.HR;

public interface IAttendanceRepository
{
    Task<AttendanceRecord?> GetAsync(Guid employeeId, DateTime date, CancellationToken cancellationToken = default);
    Task<IEnumerable<AttendanceRecord>> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<IEnumerable<AttendanceRecord>> GetByEmployeeMonthAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts or updates records matched on (EmployeeId, Date) in a single transaction.
    /// </summary>
    Task UpsertRangeAsync(IEnumerable<AttendanceRecord> records, string modifiedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fills the inclusive [fromDate, toDate] range with LEAVE marks for the employee, but ONLY on
    /// days that have no attendance record yet. Days already marked (e.g. a manual PRESENT) keep
    /// their mark so an approved leave can't rewrite real attendance or payroll history.
    /// Leaves a SaveChanges boundary for the caller's transaction.
    /// </summary>
    Task ApplyLeaveMarksAsync(Guid employeeId, DateTime fromDate, DateTime toDate, string leaveType,
        string modifiedBy, CancellationToken cancellationToken = default);
}
