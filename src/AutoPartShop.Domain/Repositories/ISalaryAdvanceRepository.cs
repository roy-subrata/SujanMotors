using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Domain.Repositories;

public interface ISalaryAdvanceRepository
{
    Task<SalaryAdvance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<SalaryAdvance>> GetOutstandingByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, decimal>> GetOutstandingTotalsAsync(CancellationToken cancellationToken = default);

    /// <summary>Records the advance and posts its cash-book expense in one transaction.</summary>
    Task GiveAsync(SalaryAdvance advance, DailyExpense expense, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes an OUTSTANDING advance and its posted expense in one transaction.</summary>
    Task CancelAsync(SalaryAdvance advance, CancellationToken cancellationToken = default);

    /// <summary>Marks all OUTSTANDING advances of the given employees as settled by the payroll run.</summary>
    Task SettleForRunAsync(Guid payrollRunId, IEnumerable<Guid> employeeIds, string settledBy, CancellationToken cancellationToken = default);
}
