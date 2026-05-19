using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class DailyExpenseRepository(AutoPartDbContext _db) : IDailyExpenseRepository
{
    public async Task<IEnumerable<DailyExpense>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.DailyExpenses
            .Where(x => !x.Isdeleted)
            .OrderByDescending(x => x.ExpenseDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<DailyExpense?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.DailyExpenses
            .FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task AddAsync(DailyExpense entity, CancellationToken cancellationToken = default)
    {
        _db.DailyExpenses.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(DailyExpense entity, CancellationToken cancellationToken = default)
    {
        _db.DailyExpenses.Update(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.DailyExpenses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity != null)
        {
            entity.Isdeleted = true;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _db.DailyExpenses.AnyAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);

    public async Task<IEnumerable<DailyExpense>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
        => await _db.DailyExpenses
            .Where(x => x.Category == category.ToUpper() && !x.Isdeleted)
            .OrderByDescending(x => x.ExpenseDate)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<DailyExpense>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        => await _db.DailyExpenses
            .Where(x => x.ExpenseDate >= startDate && x.ExpenseDate <= endDate && !x.Isdeleted)
            .OrderByDescending(x => x.ExpenseDate)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<DailyExpense>> GetByVendorAsync(string vendorName, CancellationToken cancellationToken = default)
        => await _db.DailyExpenses
            .Where(x => x.VendorName.Contains(vendorName) && !x.Isdeleted)
            .OrderByDescending(x => x.ExpenseDate)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<DailyExpense>> GetRecurringExpensesAsync(CancellationToken cancellationToken = default)
        => await _db.DailyExpenses
            .Where(x => x.IsRecurring && !x.Isdeleted)
            .OrderByDescending(x => x.ExpenseDate)
            .ToListAsync(cancellationToken);

    public async Task<decimal> GetTotalByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var total = await _db.DailyExpenses
            .Where(x => x.ExpenseDate >= startDate && x.ExpenseDate <= endDate && !x.Isdeleted)
            .SumAsync(x => x.Amount, cancellationToken);
        return total;
    }

    public async Task<decimal> GetTotalByCategoryAsync(string category, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var total = await _db.DailyExpenses
            .Where(x => x.Category == category.ToUpper() &&
                       x.ExpenseDate >= startDate &&
                       x.ExpenseDate <= endDate &&
                       !x.Isdeleted)
            .SumAsync(x => x.Amount, cancellationToken);
        return total;
    }

    public async Task<Dictionary<string, decimal>> GetTotalByCategoriesAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var totals = await _db.DailyExpenses
            .Where(x => x.ExpenseDate >= startDate && x.ExpenseDate <= endDate && !x.Isdeleted)
            .GroupBy(x => x.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);

        return totals.ToDictionary(x => x.Category, x => x.Total);
    }

    public async Task<(IEnumerable<DailyExpense> expenses, int totalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _db.DailyExpenses.Where(x => !x.Isdeleted);
        var totalCount = await query.CountAsync(cancellationToken);

        var expenses = await query
            .OrderByDescending(x => x.ExpenseDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (expenses, totalCount);
    }

    public async Task<(IEnumerable<DailyExpense> expenses, int totalCount)> GetByDateRangePagedAsync(
        DateTime startDate, DateTime endDate, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _db.DailyExpenses
            .Where(x => x.ExpenseDate >= startDate && x.ExpenseDate <= endDate && !x.Isdeleted);

        var totalCount = await query.CountAsync(cancellationToken);

        var expenses = await query
            .OrderByDescending(x => x.ExpenseDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (expenses, totalCount);
    }
}
