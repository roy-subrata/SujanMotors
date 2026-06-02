using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class ProductEmbeddingRepository(AutoPartDbContext _db) : IProductEmbeddingRepository
{
    public async Task<ProductEmbedding?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _db.ProductEmbeddings
            .FirstOrDefaultAsync(e => e.ProductId == productId && !e.Isdeleted, cancellationToken);
    }

    /// <summary>
    /// Persists the embedding. Callers either pass a freshly <c>Create</c>d instance (detached → inserted)
    /// or the tracked instance returned by <see cref="GetByProductIdAsync"/> after mutating it via
    /// <c>Update(...)</c> (tracked → changes saved).
    /// </summary>
    public async Task UpsertAsync(ProductEmbedding embedding, CancellationToken cancellationToken = default)
    {
        if (_db.Entry(embedding).State == EntityState.Detached)
            _db.ProductEmbeddings.Add(embedding);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
