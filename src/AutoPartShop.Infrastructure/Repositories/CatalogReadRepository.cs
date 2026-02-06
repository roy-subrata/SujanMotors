using AutoPartShop.Application.Catalog;
using AutoPartShop.Application.Catalog.Dtos;
using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class CatalogReadRepository(AutoPartDbContext _db) : ICatalogReadRepository
{
    public async Task<CatalogLandingResponse> GetLandingAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _db.Categories
            .Where(c => !c.Isdeleted && c.IsActive && c.ParentCategoryId == null)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new CatalogCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                ParentCategoryId = c.ParentCategoryId,
                DepthLevel = c.DepthLevel,
                DisplayOrder = c.DisplayOrder,
                ChildCount = c.ChildCount
            })
            .ToListAsync(cancellationToken);

        var baseQuery = _db.Parts
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.CatalogEntry)
            .Where(p => !p.Isdeleted && p.IsActive && p.CatalogEntry != null && p.CatalogEntry.IsPublished);

        var featured = await baseQuery
            .Where(p => p.CatalogEntry != null && p.CatalogEntry.IsFeatured)
            .OrderBy(p => p.CatalogEntry!.FeaturedRank)
            .Take(12)
            .Select(p => new CatalogProductListItem
            {
                PartId = p.Id,
                VariantId = null,
                Name = p.Name,
                CategoryName = p.Category != null ? p.Category.Name : string.Empty,
                BrandName = p.Brand != null ? p.Brand.Name : null,
                Price = p.SellingPrice,
                Currency = "BDT",
                InStock = false,
                Slug = p.CatalogEntry != null ? p.CatalogEntry.Slug : string.Empty,
                PrimaryImageUrl = p.CatalogEntry != null ? p.CatalogEntry.PrimaryImageUrl : string.Empty
            })
            .ToListAsync(cancellationToken);

        var popular = await baseQuery
            .OrderByDescending(p => p.CatalogEntry!.PopularityScore)
            .Take(12)
            .Select(p => new CatalogProductListItem
            {
                PartId = p.Id,
                VariantId = null,
                Name = p.Name,
                CategoryName = p.Category != null ? p.Category.Name : string.Empty,
                BrandName = p.Brand != null ? p.Brand.Name : null,
                Price = p.SellingPrice,
                Currency = "BDT",
                InStock = false,
                Slug = p.CatalogEntry != null ? p.CatalogEntry.Slug : string.Empty,
                PrimaryImageUrl = p.CatalogEntry != null ? p.CatalogEntry.PrimaryImageUrl : string.Empty
            })
            .ToListAsync(cancellationToken);

        var latest = await baseQuery
            .OrderByDescending(p => p.CatalogEntry!.PublishedAt ?? p.CreatedDate)
            .Take(12)
            .Select(p => new CatalogProductListItem
            {
                PartId = p.Id,
                VariantId = null,
                Name = p.Name,
                CategoryName = p.Category != null ? p.Category.Name : string.Empty,
                BrandName = p.Brand != null ? p.Brand.Name : null,
                Price = p.SellingPrice,
                Currency = "BDT",
                InStock = false,
                Slug = p.CatalogEntry != null ? p.CatalogEntry.Slug : string.Empty,
                PrimaryImageUrl = p.CatalogEntry != null ? p.CatalogEntry.PrimaryImageUrl : string.Empty
            })
            .ToListAsync(cancellationToken);

        return new CatalogLandingResponse
        {
            Categories = categories,
            Featured = featured,
            Popular = popular,
            Latest = latest
        };
    }

    public async Task<CatalogFilterResponse> GetFiltersAsync(Guid categoryId, bool includeDescendants, CancellationToken cancellationToken = default)
    {
        var categoryIds = await GetCategoryIdsAsync(categoryId, includeDescendants, cancellationToken);

        var categoryAttributes = await _db.CategoryAttributes
            .Include(ca => ca.Attribute)
            .Where(ca => !ca.Isdeleted && ca.IsFilterable && categoryIds.Contains(ca.CategoryId))
            .OrderBy(ca => ca.SortOrder)
            .ToListAsync(cancellationToken);

        var variantIdsQuery = _db.ProductVariants
            .Where(v => !v.Isdeleted && v.IsActive
                && v.Part != null
                && v.Part.CatalogEntry != null
                && v.Part.CatalogEntry.IsPublished
                && categoryIds.Contains(v.Part.CategoryId))
            .Select(v => v.Id);

        var filters = new List<CatalogFilterDto>();

        foreach (var ca in categoryAttributes)
        {
            var attribute = ca.Attribute;
            if (attribute == null)
                continue;

            var filter = new CatalogFilterDto
            {
                AttributeId = attribute.Id,
                Name = attribute.Name,
                FilterType = ca.FilterType,
                SortOrder = ca.SortOrder,
                Unit = attribute.Unit
            };

            if (ca.FilterType == "range" || attribute.DataType == "number")
            {
                var range = await _db.VariantAttributeValues
                    .Where(v => variantIdsQuery.Contains(v.VariantId) && v.AttributeId == attribute.Id && v.ValueNumber != null)
                    .GroupBy(v => v.AttributeId)
                    .Select(g => new { Min = g.Min(x => x.ValueNumber), Max = g.Max(x => x.ValueNumber) })
                    .FirstOrDefaultAsync(cancellationToken);

                filter.Min = range?.Min;
                filter.Max = range?.Max;
            }
            else
            {
                var options = await _db.VariantAttributeValues
                    .Include(v => v.Option)
                    .Where(v => variantIdsQuery.Contains(v.VariantId) && v.AttributeId == attribute.Id)
                    .GroupBy(v => v.Option != null ? v.Option.Value : v.ValueText)
                    .Select(g => new CatalogFilterOptionDto
                    {
                        Value = g.Key ?? string.Empty,
                        Count = g.Count()
                    })
                    .OrderByDescending(o => o.Count)
                    .ToListAsync(cancellationToken);

                filter.Options = options;
            }

            filters.Add(filter);
        }

        var priceRange = await _db.ProductVariants
            .Include(v => v.Part)
            .Where(v => !v.Isdeleted && v.IsActive
                && v.Part != null
                && v.Part.CatalogEntry != null
                && v.Part.CatalogEntry.IsPublished
                && categoryIds.Contains(v.Part.CategoryId))
            .Select(v => v.SellingPrice ?? v.Part!.SellingPrice)
            .ToListAsync(cancellationToken);

        if (!priceRange.Any())
        {
            priceRange = await _db.Parts
                .Include(p => p.CatalogEntry)
                .Where(p => !p.Isdeleted && p.IsActive && p.CatalogEntry != null && p.CatalogEntry.IsPublished
                    && categoryIds.Contains(p.CategoryId))
                .Select(p => p.SellingPrice)
                .ToListAsync(cancellationToken);
        }

        return new CatalogFilterResponse
        {
            CategoryId = categoryId,
            Filters = filters,
            PriceRange = new PriceRangeFilterDto
            {
                Min = priceRange.Any() ? priceRange.Min() : null,
                Max = priceRange.Any() ? priceRange.Max() : null,
                Currency = "BDT"
            },
            Availability = new AvailabilityFilterDto { InStockAvailable = true }
        };
    }

    public async Task<(IEnumerable<CatalogProductListItem> Items, int TotalCount)> SearchAsync(
        CatalogSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var categoryIds = request.CategoryId.HasValue
            ? await GetCategoryIdsAsync(request.CategoryId.Value, request.IncludeDescendants, cancellationToken)
            : new List<Guid>();

        var term = request.Search?.Trim().ToLowerInvariant() ?? string.Empty;

        IQueryable<ProductVariant> variants = _db.ProductVariants
            .Include(v => v.Part)
                .ThenInclude(p => p.Category)
            .Include(v => v.Part)
                .ThenInclude(p => p.Brand)
            .Include(v => v.Part)
                .ThenInclude(p => p.CatalogEntry)
            .Where(v => !v.Isdeleted && v.IsActive && v.Part != null && !v.Part.Isdeleted && v.Part.IsActive
                && v.Part.CatalogEntry != null && v.Part.CatalogEntry.IsPublished);

        if (request.CategoryId.HasValue)
            variants = variants.Where(v => categoryIds.Contains(v.Part!.CategoryId));

        if (!string.IsNullOrWhiteSpace(term))
            variants = variants.Where(v => EF.Functions.Like(v.Part!.Name.ToLower(), $"%{term}%")
                || EF.Functions.Like(v.Part!.SKU.ToLower(), $"%{term}%")
                || EF.Functions.Like(v.Name.ToLower(), $"%{term}%"));

        if (request.PriceMin.HasValue)
            variants = variants.Where(v => (v.SellingPrice ?? v.Part!.SellingPrice) >= request.PriceMin.Value);

        if (request.PriceMax.HasValue)
            variants = variants.Where(v => (v.SellingPrice ?? v.Part!.SellingPrice) <= request.PriceMax.Value);

        if (request.AttributeFilters != null && request.AttributeFilters.Any())
        {
            IQueryable<Guid>? filteredVariantIds = null;

            foreach (var filter in request.AttributeFilters)
            {
                var ids = _db.VariantAttributeValues
                    .Include(v => v.Option)
                    .Where(v => v.AttributeId == filter.AttributeId)
                    .Where(v =>
                        (filter.Values != null && filter.Values.Any() &&
                            (filter.Values.Contains(v.ValueText) || (v.Option != null && filter.Values.Contains(v.Option.Value))))
                        || (filter.Min.HasValue || filter.Max.HasValue) && v.ValueNumber != null
                        || (filter.Values == null || !filter.Values.Any()) && (!filter.Min.HasValue && !filter.Max.HasValue))
                    .Where(v => !filter.Min.HasValue || v.ValueNumber >= filter.Min.Value)
                    .Where(v => !filter.Max.HasValue || v.ValueNumber <= filter.Max.Value)
                    .Select(v => v.VariantId)
                    .Distinct();

                filteredVariantIds = filteredVariantIds == null ? ids : filteredVariantIds.Intersect(ids);
            }

            if (filteredVariantIds != null)
            {
                variants = variants.Where(v => filteredVariantIds.Contains(v.Id));
            }
        }

        var baseItemsQuery = variants.Select(v => new CatalogProductListItem
        {
            PartId = v.Part!.Id,
            VariantId = v.Id,
            Name = v.Name,
            CategoryName = v.Part.Category != null ? v.Part.Category.Name : string.Empty,
            BrandName = v.Part.Brand != null ? v.Part.Brand.Name : null,
            Price = v.SellingPrice ?? v.Part.SellingPrice,
            Currency = v.Currency,
            InStock = false,
            Slug = v.Part.CatalogEntry != null ? v.Part.CatalogEntry.Slug : string.Empty,
            PrimaryImageUrl = v.Part.CatalogEntry != null ? v.Part.CatalogEntry.PrimaryImageUrl : string.Empty
        });

        var includePartsWithoutVariants = request.AttributeFilters == null || !request.AttributeFilters.Any();
        IQueryable<CatalogProductListItem> partOnlyItems = Enumerable.Empty<CatalogProductListItem>().AsQueryable();

        if (includePartsWithoutVariants)
        {
            IQueryable<Part> partsOnly = _db.Parts
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.CatalogEntry)
                .Where(p => !p.Isdeleted && p.IsActive && p.CatalogEntry != null && p.CatalogEntry.IsPublished);

            if (request.CategoryId.HasValue)
                partsOnly = partsOnly.Where(p => categoryIds.Contains(p.CategoryId));

            if (!string.IsNullOrWhiteSpace(term))
                partsOnly = partsOnly.Where(p => EF.Functions.Like(p.Name.ToLower(), $"%{term}%")
                    || EF.Functions.Like(p.SKU.ToLower(), $"%{term}%"));

            if (request.PriceMin.HasValue)
                partsOnly = partsOnly.Where(p => p.SellingPrice >= request.PriceMin.Value);

            if (request.PriceMax.HasValue)
                partsOnly = partsOnly.Where(p => p.SellingPrice <= request.PriceMax.Value);

            partsOnly = partsOnly.Where(p => !_db.ProductVariants.Any(v => v.PartId == p.Id));
            partOnlyItems = partsOnly.Select(p => new CatalogProductListItem
            {
                PartId = p.Id,
                VariantId = null,
                Name = p.Name,
                CategoryName = p.Category != null ? p.Category.Name : string.Empty,
                BrandName = p.Brand != null ? p.Brand.Name : null,
                Price = p.SellingPrice,
                Currency = "BDT",
                InStock = false,
                Slug = p.CatalogEntry != null ? p.CatalogEntry.Slug : string.Empty,
                PrimaryImageUrl = p.CatalogEntry != null ? p.CatalogEntry.PrimaryImageUrl : string.Empty
            });
        }

        var merged = baseItemsQuery.Concat(partOnlyItems);
        var totalCount = await merged.CountAsync(cancellationToken);

        var items = await merged
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        if (request.InStockOnly)
        {
            var inStockVariantIds = await GetInStockVariantIdsAsync(cancellationToken);
            var inStockPartIds = await GetInStockPartIdsAsync(cancellationToken);
            items = items.Where(i =>
                (i.VariantId != null && inStockVariantIds.Contains(i.VariantId.Value)) ||
                (i.VariantId == null && inStockPartIds.Contains(i.PartId))
            ).ToList();
        }

        return (items, totalCount);
    }

    public async Task<CatalogProductDetail?> GetProductDetailAsync(Guid partId, CancellationToken cancellationToken = default)
    {
        var part = await _db.Parts
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.CatalogEntry)
            .Include(p => p.Media)
            .FirstOrDefaultAsync(p => p.Id == partId && !p.Isdeleted && p.IsActive, cancellationToken);

        if (part == null || part.CatalogEntry == null || !part.CatalogEntry.IsPublished)
            return null;

        var variants = await _db.ProductVariants
            .Include(v => v.Attributes)
                .ThenInclude(a => a.Attribute)
            .Include(v => v.Attributes)
                .ThenInclude(a => a.Option)
            .Where(v => v.PartId == partId && !v.Isdeleted && v.IsActive)
            .ToListAsync(cancellationToken);

        var variantStock = await _db.VariantStockLevels
            .Where(s => !s.Isdeleted && s.IsActive && variants.Select(v => v.Id).Contains(s.VariantId))
            .GroupBy(s => s.VariantId)
            .Select(g => new { VariantId = g.Key, Available = g.Sum(x => x.QuantityOnHand - x.QuantityReserved) })
            .ToListAsync(cancellationToken);

        var variantStockLookup = variantStock.ToDictionary(x => x.VariantId, x => x.Available);

        var partStockAvailable = await _db.StockLevels
            .Where(s => !s.Isdeleted && s.IsActive && s.PartId == partId)
            .SumAsync(s => s.QuantityOnHand - s.QuantityReserved, cancellationToken);

        var media = part.Media
            .OrderBy(m => m.SortOrder)
            .Select(m => new CatalogMediaDto
            {
                Url = m.Url,
                MediaType = m.MediaType,
                SortOrder = m.SortOrder,
                IsPrimary = m.IsPrimary
            })
            .ToList();

        var variantDtos = variants.Select(v => new CatalogVariantDto
        {
            VariantId = v.Id,
            Name = v.Name,
            Code = v.Code,
            SKU = v.SKU,
            Price = v.SellingPrice ?? part.SellingPrice,
            Currency = v.Currency,
            InStock = variantStockLookup.TryGetValue(v.Id, out var available) ? available > 0 : partStockAvailable > 0,
            Attributes = v.Attributes.Select(a => new CatalogAttributeValueDto
            {
                AttributeId = a.AttributeId,
                AttributeName = a.Attribute?.Name ?? string.Empty,
                Value = a.Option != null ? a.Option.Value : a.ValueText,
                Unit = a.Attribute?.Unit ?? string.Empty
            }).ToList()
        }).ToList();

        var defaultAttributes = variantDtos.FirstOrDefault()?.Attributes ?? new List<CatalogAttributeValueDto>();

        var attributeGroups = await _db.ProductAttributes
            .Include(a => a.AttributeGroup)
            .Where(a => defaultAttributes.Select(d => d.AttributeId).Contains(a.Id))
            .ToListAsync(cancellationToken);

        var specs = attributeGroups
            .GroupBy(a => a.AttributeGroup?.Name ?? "General")
            .Select(g => new CatalogAttributeGroupDto
            {
                GroupName = g.Key,
                SortOrder = g.FirstOrDefault()?.AttributeGroup?.SortOrder ?? 0,
                Attributes = defaultAttributes
                    .Where(a => g.Select(x => x.Id).Contains(a.AttributeId))
                    .ToList()
            })
            .OrderBy(g => g.SortOrder)
            .ToList();

        var relatedProducts = await _db.Parts
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.CatalogEntry)
            .Where(p => p.Id != partId && p.CategoryId == part.CategoryId && p.CatalogEntry != null && p.CatalogEntry.IsPublished)
            .OrderByDescending(p => p.CatalogEntry!.PopularityScore)
            .Take(8)
            .Select(p => new CatalogProductListItem
            {
                PartId = p.Id,
                VariantId = null,
                Name = p.Name,
                CategoryName = p.Category != null ? p.Category.Name : string.Empty,
                BrandName = p.Brand != null ? p.Brand.Name : null,
                Price = p.SellingPrice,
                Currency = "BDT",
                InStock = false,
                Slug = p.CatalogEntry != null ? p.CatalogEntry.Slug : string.Empty,
                PrimaryImageUrl = p.CatalogEntry != null ? p.CatalogEntry.PrimaryImageUrl : string.Empty
            })
            .ToListAsync(cancellationToken);

        return new CatalogProductDetail
        {
            PartId = part.Id,
            Name = part.Name,
            Description = part.Description,
            ShortDescription = part.CatalogEntry.ShortDescription,
            CategoryName = part.Category?.Name ?? string.Empty,
            BrandName = part.Brand?.Name,
            InStock = partStockAvailable > 0 || variantDtos.Any(v => v.InStock),
            Slug = part.CatalogEntry.Slug,
            PrimaryImageUrl = part.CatalogEntry.PrimaryImageUrl,
            Variants = variantDtos,
            Specifications = specs,
            Media = media,
            RelatedProducts = relatedProducts
        };
    }

    private async Task<List<Guid>> GetCategoryIdsAsync(Guid categoryId, bool includeDescendants, CancellationToken cancellationToken)
    {
        var categories = await _db.Categories
            .Where(c => !c.Isdeleted && c.IsActive)
            .ToListAsync(cancellationToken);

        var result = new HashSet<Guid> { categoryId };
        if (!includeDescendants)
            return result.ToList();

        var lookup = categories
            .GroupBy(c => c.ParentCategoryId)
            .Where(g => g.Key.HasValue)
            .ToDictionary(g => g.Key!.Value, g => g.ToList());
        var queue = new Queue<Guid>();
        queue.Enqueue(categoryId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!lookup.TryGetValue(current, out var children))
                continue;

            foreach (var child in children)
            {
                if (result.Add(child.Id))
                    queue.Enqueue(child.Id);
            }
        }

        return result.ToList();
    }

    private async Task<HashSet<Guid>> GetInStockVariantIdsAsync(CancellationToken cancellationToken)
    {
        var inStock = await _db.VariantStockLevels
            .Where(s => !s.Isdeleted && s.IsActive)
            .GroupBy(s => s.VariantId)
            .Select(g => new { VariantId = g.Key, Available = g.Sum(x => x.QuantityOnHand - x.QuantityReserved) })
            .Where(x => x.Available > 0)
            .Select(x => x.VariantId)
            .ToListAsync(cancellationToken);

        return inStock.ToHashSet();
    }

    private async Task<HashSet<Guid>> GetInStockPartIdsAsync(CancellationToken cancellationToken)
    {
        var inStock = await _db.StockLevels
            .Where(s => !s.Isdeleted && s.IsActive)
            .GroupBy(s => s.PartId)
            .Select(g => new { PartId = g.Key, Available = g.Sum(x => x.QuantityOnHand - x.QuantityReserved) })
            .Where(x => x.Available > 0)
            .Select(x => x.PartId)
            .ToListAsync(cancellationToken);

        return inStock.ToHashSet();
    }
}
