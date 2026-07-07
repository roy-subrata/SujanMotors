using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class PayrollRepository : IPayrollRepository
{
    private readonly AutoPartDbContext _dbContext;

    public PayrollRepository(AutoPartDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<PayrollRun>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.PayrollRuns
            .Where(x => !x.Isdeleted)
            .OrderByDescending(x => x.Year)
            .ThenByDescending(x => x.Month)
            .ToListAsync(cancellationToken);
    }

    public async Task<PayrollRun?> GetByIdAsync(Guid id, bool includePayslips = false, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.PayrollRuns.Where(x => x.Id == id && !x.Isdeleted);
        if (includePayslips)
            query = query.Include(x => x.Payslips.Where(p => !p.Isdeleted).OrderBy(p => p.EmployeeName));

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PayrollRun?> GetByYearMonthAsync(int year, int month, bool includePayslips = false, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.PayrollRuns.Where(x => x.Year == year && x.Month == month && !x.Isdeleted);
        if (includePayslips)
            query = query.Include(x => x.Payslips.Where(p => !p.Isdeleted).OrderBy(p => p.EmployeeName));

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(PayrollRun entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        await _dbContext.PayrollRuns.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PayrollRun entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        // Update() would force-mark the whole graph Modified, turning freshly added
        // payslips (client-generated IDs) into UPDATEs that hit 0 rows. Tracked
        // entities just need change detection via SaveChanges.
        if (_dbContext.Entry(entity).State == EntityState.Detached)
            _dbContext.PayrollRuns.Update(entity);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveGeneratedAsync(PayrollRun run, IReadOnlyCollection<Payslip> newPayslips, CancellationToken cancellationToken = default)
    {
        if (run == null) throw new ArgumentNullException(nameof(run));

        _dbContext.Payslips.AddRange(newPayslips);

        if (_dbContext.Entry(run).State == EntityState.Detached)
            _dbContext.PayrollRuns.Update(run);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.PayrollRuns
            .Include(x => x.Payslips)
            .FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);

        if (entity != null)
        {
            entity.Isdeleted = true;
            foreach (var payslip in entity.Payslips)
                payslip.Isdeleted = true;

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task PayAsync(PayrollRun run, DailyExpense expense, string paidBy, string paymentMethod,
        IEnumerable<Guid> employeeIdsToSettleAdvances, CancellationToken cancellationToken = default)
    {
        if (run == null) throw new ArgumentNullException(nameof(run));
        if (expense == null) throw new ArgumentNullException(nameof(expense));

        var settleIds = employeeIdsToSettleAdvances.ToList();

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            await _dbContext.DailyExpenses.AddAsync(expense, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            run.MarkPaid(paidBy, paymentMethod, expense.Id);

            if (settleIds.Count > 0)
            {
                var outstanding = await _dbContext.SalaryAdvances
                    .Where(a => settleIds.Contains(a.EmployeeId) && a.Status == "OUTSTANDING" && !a.Isdeleted)
                    .ToListAsync(cancellationToken);

                foreach (var advance in outstanding)
                {
                    advance.Settle(run.Id);
                    advance.ModifiedBy = paidBy;
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }
}
