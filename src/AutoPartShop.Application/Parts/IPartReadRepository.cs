using AutoPartShop.Application.Parts.Dtos;

namespace AutoPartShop.Application.Parts
{
    public interface IProductReadRepository
    {
        Task<(IEnumerable<ProductResponse> Parts, int TotalCount)> FindAllAsync(ProductQuery query, CancellationToken cancellationToken = default);
        Task<(IEnumerable<ProductPublicResponse> Parts, int TotalCount)> FindAllPublicAsync(ProductQuery query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Semantic search: ranks products by cosine distance between their stored embedding and
        /// <paramref name="queryVector"/> (server-side, SQL Server 2025 VECTOR_DISTANCE), paginated.
        /// </summary>
        Task<(IEnumerable<ProductResponse> Parts, int TotalCount)> SearchSemanticAsync(
            float[] queryVector, bool? isActive, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    }
}
