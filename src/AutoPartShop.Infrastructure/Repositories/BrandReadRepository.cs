using AutoPartShop.Application.Brands.Dtos;
using AutoPartShop.Application.DTOs.BrandDtos;
using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Application.Brands;

public class BrandReadRepository(AutoPartDbContext dbContext) : IBrandReadRepository
{
    public async Task<(List<BrandResponse> brands, int total)> FindAllyAsync(BrandQuery query, CancellationToken cancellationToken = default)
    {
        var brands = dbContext.Brands.Where(b => !b.Isdeleted);

        // Apply search filter
        if (!string.IsNullOrEmpty(query.Search))
        {
            var lowerTerm = query.Search.ToLower();
            brands = brands.Where(b =>
                EF.Functions.Like(b.Name, $"%{lowerTerm}%") ||
                EF.Functions.Like(b.Code, $"%{lowerTerm}%") ||
                EF.Functions.Like(b.Description, $"%{lowerTerm}%") ||
                EF.Functions.Like(b.Country, $"%{lowerTerm}%")
            );
        }

        // Apply country filter
        if (!string.IsNullOrEmpty(query.Country))
        {
            brands = brands.Where(b =>
                EF.Functions.Like(b.Country, $"%{query.Country.ToLower()}%")
            );
        }

        // Apply status filter
        if (query.IsActive.HasValue)
        {
            brands = brands.Where(b => b.IsActive == query.IsActive.Value);
        }

        // Apply sorting
        if (query.Sorts != null && query.Sorts.Count > 0)
        {
            var firstSort = query.Sorts[0];
            IOrderedQueryable<Brand> orderedBrands;

            orderedBrands = firstSort.Field.ToLower() switch
            {
                "name" => firstSort.IsAscending ? brands.OrderBy(b => b.Name) : brands.OrderByDescending(b => b.Name),
                "code" => firstSort.IsAscending ? brands.OrderBy(b => b.Code) : brands.OrderByDescending(b => b.Code),
                "country" => firstSort.IsAscending ? brands.OrderBy(b => b.Country) : brands.OrderByDescending(b => b.Country),
                "displayorder" => firstSort.IsAscending ? brands.OrderBy(b => b.DisplayOrder) : brands.OrderByDescending(b => b.DisplayOrder),
                "isactive" => firstSort.IsAscending ? brands.OrderBy(b => b.IsActive) : brands.OrderByDescending(b => b.IsActive),
                "createddate" => firstSort.IsAscending ? brands.OrderBy(b => b.CreatedDate) : brands.OrderByDescending(b => b.CreatedDate),
                _ => brands.OrderBy(b => b.DisplayOrder).ThenBy(b => b.Name)
            };

            // Apply additional sorts
            for (int i = 1; i < query.Sorts.Count; i++)
            {
                var sort = query.Sorts[i];
                orderedBrands = sort.Field.ToLower() switch
                {
                    "name" => sort.IsAscending ? orderedBrands.ThenBy(b => b.Name) : orderedBrands.ThenByDescending(b => b.Name),
                    "code" => sort.IsAscending ? orderedBrands.ThenBy(b => b.Code) : orderedBrands.ThenByDescending(b => b.Code),
                    "country" => sort.IsAscending ? orderedBrands.ThenBy(b => b.Country) : orderedBrands.ThenByDescending(b => b.Country),
                    "displayorder" => sort.IsAscending ? orderedBrands.ThenBy(b => b.DisplayOrder) : orderedBrands.ThenByDescending(b => b.DisplayOrder),
                    "isactive" => sort.IsAscending ? orderedBrands.ThenBy(b => b.IsActive) : orderedBrands.ThenByDescending(b => b.IsActive),
                    "createddate" => sort.IsAscending ? orderedBrands.ThenBy(b => b.CreatedDate) : orderedBrands.ThenByDescending(b => b.CreatedDate),
                    _ => orderedBrands
                };
            }

            brands = orderedBrands;
        }
        else
        {
            // Default sorting
            brands = brands.OrderBy(b => b.DisplayOrder).ThenBy(b => b.Name);
        }

        var totalCount = await brands.CountAsync(cancellationToken);

        var items = await brands
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items.Select(MapToResponse).ToList(), totalCount);
    }

    private static BrandResponse MapToResponse(Brand brand)
    {
        return new BrandResponse
        {
            Id = brand.Id,
            Name = brand.Name,
            Code = brand.Code,
            Description = brand.Description,
            LogoUrl = brand.LogoUrl,
            Website = brand.Website,
            Country = brand.Country,
            ContactEmail = brand.ContactEmail,
            ContactPhone = brand.ContactPhone,
            DisplayOrder = brand.DisplayOrder,
            IsActive = brand.IsActive,
            CreatedAt = brand.CreatedDate,
            ModifiedAt = brand.ModifiedDate
        };
    }
}
