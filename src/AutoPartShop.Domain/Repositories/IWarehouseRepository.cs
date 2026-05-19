
using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Domain.Repositories;

public interface IWarehouseRepository : IBaseRepository<Warehouse>
{
    Task<bool> CodeExistsAsync(string code, Guid? excludeWarehouseId = null, CancellationToken cancellationToken = default);
    Task<Warehouse?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
}
