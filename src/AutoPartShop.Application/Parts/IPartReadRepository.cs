using AutoPartShop.Application.Parts.Dtos;

namespace AutoPartShop.Application.Parts
{
    public interface IProductReadRepository
    {
        Task<(IEnumerable<ProductResponse> Parts, int TotalCount)> FindAllAsync(ProductQuery query, CancellationToken cancellationToken = default);
        Task<(IEnumerable<ProductPublicResponse> Parts, int TotalCount)> FindAllPublicAsync(ProductQuery query, CancellationToken cancellationToken = default);
    }
}
