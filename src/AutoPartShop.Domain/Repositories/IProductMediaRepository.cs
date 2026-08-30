using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Domain.Repositories;

public interface IProductMediaRepository
{
    Task<IEnumerable<ProductMedia>> GetByPartAsync(Guid partId, CancellationToken cancellationToken = default);
    Task<ProductMedia?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(ProductMedia entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(ProductMedia entity, CancellationToken cancellationToken = default);
    /// <summary>Persists several rows in one SaveChanges — used by reorder so a failure cannot leave a half-applied order.</summary>
    Task UpdateRangeAsync(IEnumerable<ProductMedia> entities, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Clears IsPrimary on every media row of the part except <paramref name="exceptId"/>.</summary>
    Task ClearPrimaryAsync(Guid partId, Guid? exceptId = null, CancellationToken cancellationToken = default);
}
