using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;


public interface ISalesOrderRepository : IBaseRepository<SalesOrder>
{
    Task<SalesOrder?> GetByNumberAsync(string soNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<SalesOrder>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SalesOrder>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<IEnumerable<SalesOrder>> GetOverdueAsync(CancellationToken cancellationToken = default);
    Task<(IEnumerable<SalesOrder> orders, int totalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<(IEnumerable<SalesOrder> orders, int totalCount)> SearchPagedAsync(string searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
