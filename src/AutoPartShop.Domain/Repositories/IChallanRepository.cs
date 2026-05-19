using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Domain.Repositories;

public interface IChallanRepository : IBaseRepository<Challan>
{
    Task<Challan?> GetByNumberAsync(string challanNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<Challan>> GetBySalesOrderAsync(Guid salesOrderId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Challan>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<bool> HasPendingChallanAsync(Guid salesOrderId, CancellationToken cancellationToken = default);
}
