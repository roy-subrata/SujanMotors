using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Domain.Repositories;

public interface IBrandRepository : IBaseRepository<Brand>
{
    Task<IEnumerable<Brand>> GetActiveBrandsAsync(CancellationToken cancellationToken = default);
}
