using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Domain.Repositories;

public interface IPayrollRepository
{
    Task<IEnumerable<PayrollRun>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PayrollRun?> GetByIdAsync(Guid id, bool includePayslips = false, CancellationToken cancellationToken = default);
    Task<PayrollRun?> GetByYearMonthAsync(int year, int month, bool includePayslips = false, CancellationToken cancellationToken = default);
    Task AddAsync(PayrollRun entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(PayrollRun entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a (re)generated run: explicitly inserts the new payslips (their IDs are
    /// client-generated, so relying on graph discovery would mark them Modified) and
    /// saves the run's totals in the same SaveChanges.
    /// </summary>
    Task SaveGeneratedAsync(PayrollRun run, IReadOnlyCollection<Payslip> newPayslips, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the run PAID, records the salary expense, and settles the outstanding
    /// salary advances of the given employees — all in one transaction so the cash
    /// book, advances and payroll can never disagree.
    /// </summary>
    Task PayAsync(PayrollRun run, DailyExpense expense, string paidBy, string paymentMethod,
        IEnumerable<Guid> employeeIdsToSettleAdvances, CancellationToken cancellationToken = default);
}
