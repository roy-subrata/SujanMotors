

using AutoPartShop.Application.Categories.Dtos;

namespace AutoPartShop.Application.Catgories;

public interface ICategoryReadRepository
{
    Task<(List<CategoryResponse> categoryResponse, int total)> FindAllyAsync(CategoryQuery query, CancellationToken cancellationToken = default);
}

