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
}
