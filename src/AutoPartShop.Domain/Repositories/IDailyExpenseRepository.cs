using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;

namespace AutoPartShop.Domain.Repositories;

public interface IDailyExpenseRepository : IBaseRepository<DailyExpense>
{
    Task<IEnumerable<DailyExpense>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);
    Task<IEnumerable<DailyExpense>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<DailyExpense>> GetByVendorAsync(string vendorName, CancellationToken cancellationToken = default);
    Task<IEnumerable<DailyExpense>> GetRecurringExpensesAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetTotalByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalByCategoryAsync(string category, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<Dictionary<string, decimal>> GetTotalByCategoriesAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<(IEnumerable<DailyExpense> expenses, int totalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<(IEnumerable<DailyExpense> expenses, int totalCount)> GetByDateRangePagedAsync(DateTime startDate, DateTime endDate, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
