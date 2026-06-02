using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Domain.Repositories;

public interface IProductEmbeddingRepository
{
    Task<ProductEmbedding?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>Insert a new embedding for the product, or update the existing one.</summary>
    Task UpsertAsync(ProductEmbedding embedding, CancellationToken cancellationToken = default);
}
