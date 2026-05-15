using AutoPartShop.Application.Brands.Dtos;
using AutoPartShop.Application.DTOs.BrandDtos;
using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Application.Brands;

public interface IBrandReadRepository
{
    Task<(List<BrandResponse> brands, int total)> FindAllyAsync(BrandQuery query, CancellationToken cancellationToken = default);
}
