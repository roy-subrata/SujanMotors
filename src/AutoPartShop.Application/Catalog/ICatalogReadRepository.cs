using AutoPartShop.Application.Catalog.Dtos;

namespace AutoPartShop.Application.Catalog;

public interface ICatalogReadRepository
{
    Task<CatalogLandingResponse> GetLandingAsync(CancellationToken cancellationToken = default);
    Task<CatalogFilterResponse> GetFiltersAsync(Guid categoryId, bool includeDescendants, CancellationToken cancellationToken = default);
    Task<(IEnumerable<CatalogProductListItem> Items, int TotalCount)> SearchAsync(CatalogSearchRequest request, CancellationToken cancellationToken = default);
    Task<CatalogProductDetail?> GetProductDetailAsync(Guid partId, CancellationToken cancellationToken = default);
}
