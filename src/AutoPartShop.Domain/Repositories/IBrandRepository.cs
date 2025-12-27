using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Domain.Repositories;

public interface IBrandRepository : IBaseRepository<Brand>
{
    Task<Brand?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IEnumerable<Brand>> GetActiveBrandsAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);
}
