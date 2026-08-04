using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Enums;

namespace AutoPartShop.Domain.Repositories;

public interface IChallanRepository : IBaseRepository<Challan>
{
    Task<Challan?> GetByNumberAsync(string challanNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<Challan>> GetBySalesOrderAsync(Guid salesOrderId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Challan>> GetByStatusAsync(ChallanStatus status, CancellationToken cancellationToken = default);
    Task<bool> HasPendingChallanAsync(Guid salesOrderId, CancellationToken cancellationToken = default);
}
