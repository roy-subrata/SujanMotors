using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;


public interface IPriceHistoryRepository : IBaseRepository<PriceHistory>
{
    Task<IEnumerable<PriceHistory>> GetByPartAsync(Guid partId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PriceHistory>> GetByPartAndDateRangeAsync(Guid partId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<PriceHistory?> GetLatestByPartAsync(Guid partId, CancellationToken cancellationToken = default);
    Task<decimal> GetPriceAtDateAsync(Guid partId, DateTime date, CancellationToken cancellationToken = default);
    Task<IEnumerable<PriceHistory>> GetByReasonAsync(string reason, CancellationToken cancellationToken = default);
    Task<(IEnumerable<PriceHistory> history, int totalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
