using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Entities.HR;

namespace AutoPartShop.Domain.Repositories.HR;

public interface ISalaryAdvanceRepository
{
    Task<SalaryAdvance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<SalaryAdvance>> GetOutstandingByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, decimal>> GetOutstandingTotalsAsync(CancellationToken cancellationToken = default);

    /// <summary>Records a REQUESTED advance. No cash moves until it is approved.</summary>
    Task RequestAsync(SalaryAdvance advance, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves the advance and posts its cash-book expense in one transaction — the point at
    /// which money actually leaves the till.
    /// </summary>
    Task ApproveAsync(SalaryAdvance advance, DailyExpense expense, CancellationToken cancellationToken = default);

    /// <summary>Persists a rejection. Nothing was ever posted, so there is no expense to reverse.</summary>
    Task RejectAsync(SalaryAdvance advance, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes a REQUESTED or OUTSTANDING advance and its posted expense, if any.</summary>
    Task CancelAsync(SalaryAdvance advance, CancellationToken cancellationToken = default);

    /// <summary>Marks all OUTSTANDING advances of the given employees as settled by the payroll run.</summary>
    Task SettleForRunAsync(Guid payrollRunId, IEnumerable<Guid> employeeIds, string settledBy, CancellationToken cancellationToken = default);
}
