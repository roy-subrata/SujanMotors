using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Entities.HR;
using AutoPartShop.Domain.Enums.HR;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Domain.Repositories.HR;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories.HR;

public class SalaryAdvanceRepository : ISalaryAdvanceRepository
{
    private readonly AutoPartDbContext _dbContext;
    private readonly IDailyExpenseRepository _dailyExpenseRepository;

    public SalaryAdvanceRepository(AutoPartDbContext dbContext, IDailyExpenseRepository dailyExpenseRepository)
    {
        _dbContext = dbContext;
        _dailyExpenseRepository = dailyExpenseRepository;
    }

    public async Task<SalaryAdvance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SalaryAdvances
            .FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task<IEnumerable<SalaryAdvance>> GetOutstandingByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SalaryAdvances
            .Where(x => x.EmployeeId == employeeId && x.Status == SalaryAdvanceStatus.OUTSTANDING && !x.Isdeleted)
            .OrderBy(x => x.AdvanceDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, decimal>> GetOutstandingTotalsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SalaryAdvances
            .Where(x => x.Status == SalaryAdvanceStatus.OUTSTANDING && !x.Isdeleted)
            .GroupBy(x => x.EmployeeId)
            .Select(g => new { g.Key, Total = g.Sum(x => x.Amount - x.RecoveredAmount) })
            .ToDictionaryAsync(x => x.Key, x => x.Total, cancellationToken);
    }

    public async Task RequestAsync(SalaryAdvance advance, CancellationToken cancellationToken = default)
    {
        if (advance == null) throw new ArgumentNullException(nameof(advance));

        await _dbContext.SalaryAdvances.AddAsync(advance, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectAsync(SalaryAdvance advance, CancellationToken cancellationToken = default)
    {
        if (advance == null) throw new ArgumentNullException(nameof(advance));

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ApproveAsync(SalaryAdvance advance, DailyExpense expense, CancellationToken cancellationToken = default)
    {
        if (advance == null) throw new ArgumentNullException(nameof(advance));
        if (expense == null) throw new ArgumentNullException(nameof(expense));

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            await _dailyExpenseRepository.AddAsync(expense, cancellationToken);

            // The advance already exists (it was REQUESTED); approving links the expense and
            // flips the status, so this is an update rather than an insert.
            advance.LinkExpense(expense.Id);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        });
    }

    public async Task CancelAsync(SalaryAdvance advance, CancellationToken cancellationToken = default)
    {
        if (advance == null) throw new ArgumentNullException(nameof(advance));

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            advance.Isdeleted = true;

            if (advance.ExpenseId is Guid expenseId)
            {
                var expense = await _dbContext.DailyExpenses
                    .FirstOrDefaultAsync(x => x.Id == expenseId, cancellationToken);
                if (expense != null)
                    expense.Isdeleted = true;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }

    public async Task SettleForRunAsync(Guid payrollRunId, IEnumerable<Guid> employeeIds, string settledBy, CancellationToken cancellationToken = default)
    {
        var ids = employeeIds.ToList();
        var outstanding = await _dbContext.SalaryAdvances
            .Where(x => ids.Contains(x.EmployeeId) && x.Status == SalaryAdvanceStatus.OUTSTANDING && !x.Isdeleted)
            .ToListAsync(cancellationToken);

        foreach (var advance in outstanding)
        {
            advance.Settle(payrollRunId);
            advance.ModifiedBy = settledBy;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
