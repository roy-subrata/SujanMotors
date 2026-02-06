using AutoPartShop.Application.Parts;
using AutoPartShop.Application.Parts.Dtos;
using AutoPartsShop.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class PartReadRepository(AutoPartDbContext _db) : IPartReadRepository
{
    public async Task<(IEnumerable<PartResponse> Parts, int TotalCount)> FindAllAsync(PartQuery query, CancellationToken cancellationToken = default)
    {
        var term = query.Search.ToLower();

        var parts = _db.Parts
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Unit)
            .Where(x => !x.Isdeleted && x.IsActive == query.IsActive && (
             (EF.Functions.Like(x.Name, $"%{term}%") ||
             EF.Functions.Like(x.SKU, $"%{term}%")
            )));


        if (query.Sorts != null && query.Sorts.Any())
        {
            var sorts =
                query.Sorts.Select(x => (x.Field, x.Direction == "asc" ? true : false)).ToArray();
            parts = parts.OrderByMultiple(sorts);
        }
        else
        {
            parts = parts.OrderBy(x => x.CreatedDate);
        }

        var totalCount = await parts.CountAsync(cancellationToken);
        var items = await parts
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
             .Select(part => new PartResponse()
             {
                 Id = part.Id,
                 Name = part.Name,
                 Description = part.Description,
                 PartNumber = part.PartNumber.Value,
                 SKU = part.SKU,
                 CategoryId = part.CategoryId,
                 CategoryName = part.Category.Name ?? string.Empty,
                 BrandId = part.BrandId,
                 BrandName = part.Brand.Name,
                 BrandCode = part.Brand.Code,
                 UnitId = part.UnitId,
                 UnitName = part.Unit.Name,
                 CostPrice = part.CostPrice,
                 SellingPrice = part.SellingPrice,
                 MinimumStock = part.MinimumStock,
                 IsActive = part.IsActive,
                 MinMarginPercentOverride = part.MinMarginPercentOverride,
                 MaxDiscountPercentOverride = part.MaxDiscountPercentOverride,
                 HasWarranty = part.HasWarranty,
                 WarrantyPeriodMonths = part.WarrantyPeriodMonths,
                 WarrantyType = part.WarrantyType,
                 WarrantyTerms = part.WarrantyTerms,
                 WarrantyCertificateTemplate = part.WarrantyCertificateTemplate,
                 CreatedBy = part.CreatedBy,
                 ModifiedBy = part.ModifiedBy

             })
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IEnumerable<PartPublicResponse> Parts, int TotalCount)> FindAllPublicAsync(PartQuery query, CancellationToken cancellationToken = default)
    {
        var term = query.Search.ToLower();

        var parts = _db.Parts
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Unit)
            .Where(x => !x.Isdeleted && x.IsActive == query.IsActive && (
             (EF.Functions.Like(x.Name, $"%{term}%") ||
             EF.Functions.Like(x.SKU, $"%{term}%")
            )));


        if (query.Sorts != null && query.Sorts.Any())
        {
            var sorts =
                query.Sorts.Select(x => (x.Field, x.Direction == "asc" ? true : false)).ToArray();
            parts = parts.OrderByMultiple(sorts);
        }
        else
        {
            parts = parts.OrderBy(x => x.CreatedDate);
        }

        var totalCount = await parts.CountAsync(cancellationToken);
        var items = await parts
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
             .Select(part => new PartPublicResponse()
             {
                 Id = part.Id,
                 Name = part.Name,
                 Description = part.Description,
                 PartNumber = part.PartNumber.Value,
                 SKU = part.SKU,
                 CategoryId = part.CategoryId,
                 CategoryName = part.Category.Name ?? string.Empty,
                 BrandId = part.BrandId,
                 BrandName = part.Brand.Name,
                 BrandCode = part.Brand.Code,
                 UnitId = part.UnitId,
                 UnitName = part.Unit.Name,
                 SellingPrice = part.SellingPrice,
                 MinimumStock = part.MinimumStock,
                 IsActive = part.IsActive,
                 MinMarginPercentOverride = part.MinMarginPercentOverride,
                 MaxDiscountPercentOverride = part.MaxDiscountPercentOverride,
                 HasWarranty = part.HasWarranty,
                 WarrantyPeriodMonths = part.WarrantyPeriodMonths,
                 WarrantyType = part.WarrantyType,
                 WarrantyTerms = part.WarrantyTerms,
                 WarrantyCertificateTemplate = part.WarrantyCertificateTemplate
             })
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
