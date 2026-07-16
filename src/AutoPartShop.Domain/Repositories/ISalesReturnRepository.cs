using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;


public interface ISalesReturnRepository : IBaseRepository<SalesReturn>
{
    Task<SalesReturn?> GetByNumberAsync(string returnNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<SalesReturn>> GetBySalesOrderAsync(Guid salesOrderId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SalesReturn>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<IEnumerable<SalesReturn>> GetPendingApprovalAsync(CancellationToken cancellationToken = default);
    Task<(IEnumerable<SalesReturn> returns, int totalCount)> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
}
