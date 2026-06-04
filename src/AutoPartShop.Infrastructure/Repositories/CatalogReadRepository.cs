using AutoPartShop.Application.Catalog;
using AutoPartShop.Application.Catalog.Dtos;
using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class CatalogReadRepository(AutoPartDbContext _db) : ICatalogReadRepository
{
    public async Task<CatalogLandingResponse> GetLandingAsync(CancellationToken cancellationToken = default)
    {
        // Fix: return ALL active categories (not just root) so frontend can build full tree
        var categories = await _db.Categories
            .Where(c => !c.Isdeleted && c.IsActive)
            .OrderBy(c => c.DepthLevel)
            .ThenBy(c => c.DisplayOrder)
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

        // Products must be published; fall back to any active product if no CatalogEntry
        var publishedParts = await _db.Parts
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.CatalogEntry)
            .Where(p => !p.Isdeleted && p.IsActive
                && (p.CatalogEntry == null || p.CatalogEntry.IsPublished))
            .ToListAsync(cancellationToken);

        var partIds = publishedParts.Select(p => p.Id).ToList();

        // Batch load stock and discounts for all products
        var stockLookup = await GetInStockPartIdsAsync(partIds, cancellationToken);
        var discountLookup = await GetActiveDiscountsForPartsAsync(partIds, cancellationToken);

        var featured = publishedParts
            .Where(p => p.CatalogEntry?.IsFeatured == true)
            .OrderBy(p => p.CatalogEntry!.FeaturedRank)
            .Take(12)
            .Select(p => ToListItem(p, stockLookup, discountLookup))
            .ToList();

        var popular = publishedParts
            .OrderByDescending(p => p.CatalogEntry?.PopularityScore ?? 0)
            .Take(12)
            .Select(p => ToListItem(p, stockLookup, discountLookup))
            .ToList();

        var latest = publishedParts
            .OrderByDescending(p => p.CatalogEntry?.PublishedAt ?? p.CreatedDate)
            .Take(12)
            .Select(p => ToListItem(p, stockLookup, discountLookup))
            .ToList();

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
                && (v.Part.CatalogEntry == null || v.Part.CatalogEntry.IsPublished)
                && categoryIds.Contains(v.Part.CategoryId))
            .Select(v => v.Id);

        var filters = new List<CatalogFilterDto>();

        foreach (var ca in categoryAttributes)
        {
            var attribute = ca.Attribute;
            if (attribute == null) continue;

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

        // Price range from variants + fallback to parts
        var variantPrices = await _db.ProductVariants
            .Include(v => v.Part)
            .Where(v => !v.Isdeleted && v.IsActive
                && v.Part != null
                && (v.Part.CatalogEntry == null || v.Part.CatalogEntry.IsPublished)
                && categoryIds.Contains(v.Part.CategoryId))
            .Select(v => v.SellingPrice > 0 ? v.SellingPrice : v.Part!.SellingPrice)
            .ToListAsync(cancellationToken);

        var partPrices = await _db.Parts
            .Where(p => !p.Isdeleted && p.IsActive
                && (p.CatalogEntry == null || p.CatalogEntry.IsPublished)
                && categoryIds.Contains(p.CategoryId))
            .Select(p => p.SellingPrice)
            .ToListAsync(cancellationToken);

        var allPrices = variantPrices.Concat(partPrices).Where(p => p > 0).ToList();

        return new CatalogFilterResponse
        {
            CategoryId = categoryId,
            Filters = filters,
            PriceRange = new PriceRangeFilterDto
            {
                Min = allPrices.Any() ? allPrices.Min() : null,
                Max = allPrices.Any() ? allPrices.Max() : null,
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

        // Vehicle compatibility filter — resolve compatible part IDs up front
        HashSet<Guid>? compatiblePartIds = null;
        if (request.VehicleId.HasValue)
        {
            var ids = await _db.PartVehicleCompatibilities
                .Where(pvc => pvc.VehicleId == request.VehicleId.Value && !pvc.Isdeleted)
                .Select(pvc => pvc.PartId)
                .ToListAsync(cancellationToken);
            compatiblePartIds = ids.ToHashSet();
        }

        // Query 1: variants with catalog-published parts
        IQueryable<ProductVariant> variants = _db.ProductVariants
            .Include(v => v.Part).ThenInclude(p => p!.Category)
            .Include(v => v.Part).ThenInclude(p => p!.Brand)
            .Include(v => v.Part).ThenInclude(p => p!.CatalogEntry)
            .Where(v => !v.Isdeleted && v.IsActive && v.Part != null && !v.Part.Isdeleted && v.Part.IsActive
                && (v.Part.CatalogEntry == null || v.Part.CatalogEntry.IsPublished));

        if (compatiblePartIds != null)
            variants = variants.Where(v => compatiblePartIds.Contains(v.PartId));

        if (request.CategoryId.HasValue)
            variants = variants.Where(v => categoryIds.Contains(v.Part!.CategoryId));

        if (!string.IsNullOrWhiteSpace(term))
            variants = variants.Where(v =>
                EF.Functions.Like(v.Part!.Name.ToLower(), $"%{term}%") ||
                EF.Functions.Like(v.Part!.SKU.ToLower(), $"%{term}%") ||
                EF.Functions.Like(v.Name.ToLower(), $"%{term}%"));

        if (request.PriceMin.HasValue)
            variants = variants.Where(v => (v.SellingPrice > 0 ? v.SellingPrice : v.Part!.SellingPrice) >= request.PriceMin.Value);

        if (request.PriceMax.HasValue)
            variants = variants.Where(v => (v.SellingPrice > 0 ? v.SellingPrice : v.Part!.SellingPrice) <= request.PriceMax.Value);

        if (request.AttributeFilters != null && request.AttributeFilters.Any())
        {
            IQueryable<Guid>? filteredVariantIds = null;
            foreach (var filter in request.AttributeFilters)
            {
                var hasValues = filter.Values != null && filter.Values.Count > 0;
                var hasRange  = filter.Min.HasValue || filter.Max.HasValue;

                // Skip filters with nothing to filter on — avoids excluding products incorrectly
                if (!hasValues && !hasRange) continue;

                IQueryable<Guid> ids;

                if (hasValues)
                {
                    // Match by option value text (supports both option-based and free-text attributes)
                    ids = _db.VariantAttributeValues
                        .Where(v => v.AttributeId == filter.AttributeId
                            && (filter.Values!.Contains(v.ValueText)
                                || (v.Option != null && filter.Values.Contains(v.Option.Value))))
                        .Select(v => v.VariantId).Distinct();
                }
                else
                {
                    // Range filter on numeric values
                    ids = _db.VariantAttributeValues
                        .Where(v => v.AttributeId == filter.AttributeId
                            && v.ValueNumber != null
                            && (!filter.Min.HasValue || v.ValueNumber >= filter.Min.Value)
                            && (!filter.Max.HasValue || v.ValueNumber <= filter.Max.Value))
                        .Select(v => v.VariantId).Distinct();
                }

                filteredVariantIds = filteredVariantIds == null ? ids : filteredVariantIds.Intersect(ids);
            }

            if (filteredVariantIds != null)
                variants = variants.Where(v => filteredVariantIds.Contains(v.Id));
        }

        var variantList = await variants.ToListAsync(cancellationToken);
        var variantPartIds = variantList.Select(v => v.PartId).Distinct().ToList();

        // Query 2: parts WITHOUT active variants — always included regardless of attribute filters
        // (non-variant parts have no VariantAttributeValues so attribute filters cannot apply to them)
        IQueryable<Product> partsOnly = _db.Parts
            .Include(p => p.Category).Include(p => p.Brand).Include(p => p.CatalogEntry)
            .Where(p => !p.Isdeleted && p.IsActive
                && (p.CatalogEntry == null || p.CatalogEntry.IsPublished)
                && !_db.ProductVariants.Any(v => v.PartId == p.Id && !v.Isdeleted && v.IsActive));

        if (request.CategoryId.HasValue)
            partsOnly = partsOnly.Where(p => categoryIds.Contains(p.CategoryId));

        if (!string.IsNullOrWhiteSpace(term))
            partsOnly = partsOnly.Where(p =>
                EF.Functions.Like(p.Name.ToLower(), $"%{term}%") ||
                EF.Functions.Like(p.SKU.ToLower(), $"%{term}%"));

        if (request.PriceMin.HasValue)
            partsOnly = partsOnly.Where(p => p.SellingPrice >= request.PriceMin.Value);

        if (request.PriceMax.HasValue)
            partsOnly = partsOnly.Where(p => p.SellingPrice <= request.PriceMax.Value);

        if (compatiblePartIds != null)
            partsOnly = partsOnly.Where(p => compatiblePartIds.Contains(p.Id));

        // Exclude parts already represented by a variant result to avoid duplicates
        partsOnly = partsOnly.Where(p => !variantPartIds.Contains(p.Id));

        var partsWithoutVariants = await partsOnly.ToListAsync(cancellationToken);

        // Batch load stock + discounts
        var allPartIds = variantPartIds.Concat(partsWithoutVariants.Select(p => p.Id)).Distinct().ToList();
        var variantIdsForStock = variantList.Select(v => v.Id).Distinct().ToList();
        var variantStockLookup = await GetInStockVariantIdsAsync(variantIdsForStock, cancellationToken);
        var stockLookup = await GetInStockPartIdsAsync(partsWithoutVariants.Select(p => p.Id).ToList(), cancellationToken);
        var discountLookup = await GetActiveDiscountsForPartsAsync(allPartIds, cancellationToken);

        // Fix 5: group variants by PartId → one card per product (use part name, cheapest in-stock variant)
        var variantItems = variantList
            .GroupBy(v => v.PartId)
            .Select(g =>
            {
                var part = g.First().Part!;
                // Prefer an in-stock variant with the lowest price; fall back to any variant
                var rep = g.Where(v => variantStockLookup.Contains(v.Id))
                            .OrderBy(v => v.SellingPrice > 0 ? v.SellingPrice : part.SellingPrice)
                            .FirstOrDefault()
                        ?? g.OrderBy(v => v.PricingMode == "ADDITIVE" ? part.SellingPrice + v.SellingPrice : v.SellingPrice).First();

                var basePrice = rep.SellingPrice > 0 ? rep.SellingPrice : part.SellingPrice;
                var item = new CatalogProductListItem
                {
                    PartId  = part.Id,
                    VariantId = rep.Id,
                    Name    = part.Name,  // Part name — variant selected on detail page
                    CategoryName   = part.Category?.Name ?? string.Empty,
                    BrandName      = part.Brand?.Name,
                    Currency       = rep.Currency,
                    InStock        = g.Any(v => variantStockLookup.Contains(v.Id)),
                    Slug           = part.CatalogEntry?.Slug ?? SlugFromName(part.Name),
                    PrimaryImageUrl = part.CatalogEntry?.PrimaryImageUrl ?? string.Empty
                };
                ApplyDiscount(item, basePrice, discountLookup);
                return item;
            })
            .ToList();

        var partItems = partsWithoutVariants.Select(p => ToListItem(p, stockLookup, discountLookup)).ToList();

        var merged = variantItems.Concat(partItems).ToList();

        // Apply InStockOnly filter before counting so pagination totals are accurate
        if (request.InStockOnly)
            merged = merged.Where(i => i.InStock).ToList();

        if (request.OnSaleOnly)
            merged = merged.Where(i => i.IsOnSale).ToList();

        var totalCount = merged.Count;
        var items = merged.Skip(request.Skip).Take(request.PageSize).ToList();
        return (items, totalCount);
    }

    public async Task<CatalogProductDetail?> GetProductDetailAsync(Guid partId, CancellationToken cancellationToken = default)
    {
        var part = await _db.Parts
            .Include(p => p.Category).Include(p => p.Brand)
            .Include(p => p.CatalogEntry).Include(p => p.Media)
            .FirstOrDefaultAsync(p => p.Id == partId && !p.Isdeleted && p.IsActive, cancellationToken);

        if (part == null) return null;
        if (part.CatalogEntry != null && !part.CatalogEntry.IsPublished) return null;

        var variants = await _db.ProductVariants
            .Include(v => v.Attributes).ThenInclude(a => a.Attribute)
            .Include(v => v.Attributes).ThenInclude(a => a.Option)
            .Where(v => v.PartId == partId && !v.Isdeleted && v.IsActive)
            .ToListAsync(cancellationToken);

        var variantIds = variants.Select(v => v.Id).ToList();

        var variantStock = await _db.StockLevels
            .Where(s => !s.Isdeleted && s.IsActive && s.VariantId != null && variantIds.Contains(s.VariantId.Value))
            .GroupBy(s => s.VariantId!.Value)
            .Select(g => new { VariantId = g.Key, Available = g.Sum(x => x.QuantityOnHand - x.QuantityReserved) })
            .ToListAsync(cancellationToken);

        var variantStockLookup = variantStock.ToDictionary(x => x.VariantId, x => x.Available);

        var partStockAvailable = await _db.StockLevels
            .Where(s => !s.Isdeleted && s.IsActive && s.PartId == partId)
            .SumAsync(s => s.QuantityOnHand - s.QuantityReserved, cancellationToken);

        // Discounts for this product
        var discounts = await _db.Discounts
            .Where(d => d.IsActive && !d.Isdeleted
                && d.StartDate.Date <= DateTime.UtcNow.Date
                && (!d.EndDate.HasValue || d.EndDate.Value.Date >= DateTime.UtcNow.Date)
                && (d.PartId == partId || d.PartId == null))
            .ToListAsync(cancellationToken);

        var media = part.Media.OrderBy(m => m.SortOrder)
            .Select(m => new CatalogMediaDto { Url = m.Url, MediaType = m.MediaType, SortOrder = m.SortOrder, IsPrimary = m.IsPrimary })
            .ToList();

        var variantDtos = variants.Select(v =>
        {
            var basePrice = v.SellingPrice > 0 ? v.SellingPrice : part.SellingPrice;
            var variantDiscount = discounts.FirstOrDefault(d => d.ProductVariantId == v.Id)
                ?? discounts.FirstOrDefault(d => d.PartId == partId && d.ProductVariantId == null)
                ?? discounts.FirstOrDefault(d => d.PartId == null);

            decimal? salePriceV = null;
            if (variantDiscount != null)
            {
                var amt = variantDiscount.CalculateDiscountAmount(basePrice);
                if (amt > 0) salePriceV = Math.Max(0, basePrice - amt);
            }

            return new CatalogVariantDto
            {
                VariantId = v.Id,
                Name = v.Name,
                Code = v.Code,
                SKU = v.SKU,
                OriginalPrice = basePrice,
                SalePrice = salePriceV,
                Price = salePriceV ?? basePrice,
                IsOnSale = salePriceV.HasValue,
                Currency = v.Currency,
                InStock = variantStockLookup.TryGetValue(v.Id, out var avail) ? avail > 0 : partStockAvailable > 0,
                Attributes = v.Attributes.Select(a => new CatalogAttributeValueDto
                {
                    AttributeId = a.AttributeId,
                    AttributeName = a.Attribute?.Name ?? string.Empty,
                    Value = a.Option != null ? a.Option.Value : a.ValueText,
                    Unit = a.Attribute?.Unit ?? string.Empty
                }).ToList()
            };
        }).ToList();

        // Load attribute group metadata for ALL variant attributes (not just first variant)
        var allAttributeIds = variantDtos
            .SelectMany(v => v.Attributes.Select(a => a.AttributeId))
            .Distinct()
            .ToList();

        var allAttributeGroups = await _db.ProductAttributes
            .Include(a => a.AttributeGroup)
            .Where(a => allAttributeIds.Contains(a.Id))
            .ToListAsync(cancellationToken);

        // Build specs per variant so UI can switch specs when variant is selected
        foreach (var vDto in variantDtos)
        {
            var variantAttrIds = vDto.Attributes.Select(a => a.AttributeId).ToHashSet();
            vDto.Specifications = allAttributeGroups
                .Where(a => variantAttrIds.Contains(a.Id))
                .GroupBy(a => a.AttributeGroup?.Name ?? "General")
                .Select(g => new CatalogAttributeGroupDto
                {
                    GroupName = g.Key,
                    SortOrder = g.FirstOrDefault()?.AttributeGroup?.SortOrder ?? 0,
                    Attributes = vDto.Attributes
                        .Where(a => g.Select(x => x.Id).Contains(a.AttributeId))
                        .ToList()
                })
                .OrderBy(g => g.SortOrder)
                .ToList();
        }

        // Product-level specs: union of all variants' specs (shown before a variant is selected)
        var defaultAttributes = variantDtos.FirstOrDefault()?.Attributes ?? new List<CatalogAttributeValueDto>();
        var specs = variantDtos.FirstOrDefault()?.Specifications ?? new List<CatalogAttributeGroupDto>();

        // Fix: compute InStock for related products properly
        var relatedParts = await _db.Parts
            .Include(p => p.Category).Include(p => p.Brand).Include(p => p.CatalogEntry)
            .Where(p => p.Id != partId && p.CategoryId == part.CategoryId && !p.Isdeleted && p.IsActive
                && (p.CatalogEntry == null || p.CatalogEntry.IsPublished))
            .OrderByDescending(p => p.CatalogEntry != null ? p.CatalogEntry.PopularityScore : 0)
            .Take(8)
            .ToListAsync(cancellationToken);

        var relatedIds = relatedParts.Select(p => p.Id).ToList();
        var relatedStock = await GetInStockPartIdsAsync(relatedIds, cancellationToken);
        var relatedDiscounts = await GetActiveDiscountsForPartsAsync(relatedIds, cancellationToken);

        var relatedProducts = relatedParts
            .Select(p => ToListItem(p, relatedStock, relatedDiscounts))
            .ToList();

        // Product-level sale price: use the best variant discount or a product-level discount
        var bestVariantForPrice = variantDtos.OrderBy(v => v.Price).FirstOrDefault();
        var productBasePrice = bestVariantForPrice?.OriginalPrice ?? part.SellingPrice;
        var productSalePrice = variantDtos.Any(v => v.IsOnSale)
            ? variantDtos.Where(v => v.IsOnSale).Min(v => v.Price)
            : (decimal?)null;

        // Fallback: apply product-level discount if no variant overrides it
        if (!productSalePrice.HasValue)
        {
            var productDiscount = discounts.FirstOrDefault(d => d.PartId == partId && d.ProductVariantId == null)
                ?? discounts.FirstOrDefault(d => d.PartId == null);
            if (productDiscount != null)
            {
                var discAmt = productDiscount.CalculateDiscountAmount(productBasePrice);
                if (discAmt > 0) productSalePrice = Math.Max(0, productBasePrice - discAmt);
            }
        }

        return new CatalogProductDetail
        {
            PartId = part.Id,
            Name = part.Name,
            Description = part.Description,
            RichDescription = part.RichDescription,
            ShortDescription = part.CatalogEntry?.ShortDescription ?? string.Empty,
            CategoryName = part.Category?.Name ?? string.Empty,
            BrandName = part.Brand?.Name,
            SKU = part.SKU,
            Tags = part.Tags,
            InStock = partStockAvailable > 0 || variantDtos.Any(v => v.InStock),
            Slug = part.CatalogEntry?.Slug ?? SlugFromName(part.Name),
            PrimaryImageUrl = part.CatalogEntry?.PrimaryImageUrl ?? string.Empty,
            BasePrice = productBasePrice,
            OriginalPrice = productBasePrice,
            SalePrice = productSalePrice,
            IsOnSale = productSalePrice.HasValue,
            Currency = variantDtos.FirstOrDefault()?.Currency ?? part.SellingPriceCurrency ?? "BDT",
            HasWarranty = part.HasWarranty,
            WarrantyPeriodMonths = part.WarrantyPeriodMonths,
            WarrantyType = part.WarrantyType,
            WarrantyTerms = part.WarrantyTerms,
            Variants = variantDtos,
            Specifications = specs,
            Media = media,
            RelatedProducts = relatedProducts
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static CatalogProductListItem ToListItem(
        Product p,
        HashSet<Guid> stockLookup,
        Dictionary<Guid, List<Discount>> discountLookup)
    {
        var item = new CatalogProductListItem
        {
            PartId = p.Id,
            VariantId = null,
            Name = p.Name,
            CategoryName = p.Category?.Name ?? string.Empty,
            BrandName = p.Brand?.Name,
            Currency = "BDT",
            InStock = stockLookup.Contains(p.Id),
            Slug = p.CatalogEntry?.Slug ?? SlugFromName(p.Name),
            PrimaryImageUrl = p.CatalogEntry?.PrimaryImageUrl ?? string.Empty
        };
        ApplyDiscount(item, p.SellingPrice, discountLookup);
        return item;
    }

    private static void ApplyDiscount(
        CatalogProductListItem item,
        decimal basePrice,
        Dictionary<Guid, List<Discount>> discountLookup)
    {
        item.OriginalPrice = basePrice;

        if (!discountLookup.TryGetValue(item.PartId, out var discounts) || !discounts.Any())
        {
            item.Price = basePrice;
            item.IsOnSale = false;
            return;
        }

        // Best discount wins (highest savings)
        var best = discounts
            .Select(d => new { Discount = d, Amount = d.CalculateDiscountAmount(basePrice) })
            .Where(x => x.Amount > 0)
            .OrderByDescending(x => x.Amount)
            .FirstOrDefault();

        if (best == null)
        {
            item.Price = basePrice;
            item.IsOnSale = false;
            return;
        }

        item.SalePrice = basePrice - best.Amount;
        item.Price = item.SalePrice.Value;
        item.IsOnSale = true;
    }

    // Availability from unified StockLevels: variant parts use variant-scoped rows (VariantId set),
    // plain parts use part-level rows (VariantId null).
    private async Task<HashSet<Guid>> GetInStockPartIdsAsync(List<Guid> partIds, CancellationToken ct)
    {
        if (!partIds.Any()) return new HashSet<Guid>();

        // Determine which parts have at least one active variant
        var partsWithVariants = await _db.ProductVariants
            .Where(v => !v.Isdeleted && v.IsActive && partIds.Contains(v.PartId))
            .Select(v => v.PartId)
            .Distinct()
            .ToListAsync(ct);

        var partsWithVariantsSet = partsWithVariants.ToHashSet();
        var partsWithoutVariants = partIds.Where(id => !partsWithVariantsSet.Contains(id)).ToList();

        var result = new HashSet<Guid>();

        // Variant products: a part is in stock if ANY of its active variants has stock
        if (partsWithVariantsSet.Any())
        {
            // Step 1 — get all variant IDs (with their parent PartId) for these parts
            var variantIdToPartId = await _db.ProductVariants
                .Where(v => !v.Isdeleted && v.IsActive && partsWithVariantsSet.Contains(v.PartId))
                .Select(v => new { v.Id, v.PartId })
                .ToListAsync(ct);

            var variantIds = variantIdToPartId.Select(x => x.Id).ToList();

            // Step 2 — find which variants have available stock
            var inStockVariantIds = await _db.StockLevels
                .Where(s => !s.Isdeleted && s.IsActive && s.VariantId != null && variantIds.Contains(s.VariantId.Value))
                .GroupBy(s => s.VariantId!.Value)
                .Select(g => new { VariantId = g.Key, Available = g.Sum(x => x.QuantityOnHand - x.QuantityReserved) })
                .Where(x => x.Available > 0)
                .Select(x => x.VariantId)
                .ToListAsync(ct);

            var inStockVariantSet = inStockVariantIds.ToHashSet();

            // Step 3 — map back to PartIds
            foreach (var x in variantIdToPartId.Where(x => inStockVariantSet.Contains(x.Id)))
                result.Add(x.PartId);
        }

        // Non-variant products: check part-level StockLevels
        if (partsWithoutVariants.Any())
        {
            var partInStock = await _db.StockLevels
                .Where(s => !s.Isdeleted && s.IsActive && partsWithoutVariants.Contains(s.PartId))
                .GroupBy(s => s.PartId)
                .Select(g => new { PartId = g.Key, Available = g.Sum(x => x.QuantityOnHand - x.QuantityReserved) })
                .Where(x => x.Available > 0)
                .Select(x => x.PartId)
                .ToListAsync(ct);

            foreach (var id in partInStock) result.Add(id);
        }

        return result;
    }

    private async Task<HashSet<Guid>> GetInStockVariantIdsAsync(List<Guid> variantIds, CancellationToken ct)
    {
        if (!variantIds.Any()) return new HashSet<Guid>();

        var inStock = await _db.StockLevels
            .Where(s => !s.Isdeleted && s.IsActive && s.VariantId != null && variantIds.Contains(s.VariantId.Value))
            .GroupBy(s => s.VariantId!.Value)
            .Select(g => new { VariantId = g.Key, Available = g.Sum(x => x.QuantityOnHand - x.QuantityReserved) })
            .Where(x => x.Available > 0)
            .Select(x => x.VariantId)
            .ToListAsync(ct);

        return inStock.ToHashSet();
    }

    private async Task<Dictionary<Guid, List<Discount>>> GetActiveDiscountsForPartsAsync(
        List<Guid> partIds, CancellationToken ct)
    {
        if (!partIds.Any()) return new Dictionary<Guid, List<Discount>>();

        var today = DateTime.UtcNow.Date;
        var discounts = await _db.Discounts
            .Where(d => d.IsActive && !d.Isdeleted
                && d.StartDate.Date <= today
                && (!d.EndDate.HasValue || d.EndDate.Value.Date >= today)
                && (d.PartId == null || partIds.Contains(d.PartId.Value)))
            .ToListAsync(ct);

        var result = new Dictionary<Guid, List<Discount>>();
        foreach (var partId in partIds)
        {
            result[partId] = discounts
                .Where(d => d.PartId == partId || (d.PartId == null && !d.IsVariantLevel))
                .ToList();
        }
        return result;
    }

    private async Task<HashSet<Guid>> GetInStockVariantIdsAsync(CancellationToken ct)
    {
        var inStock = await _db.StockLevels
            .Where(s => !s.Isdeleted && s.IsActive && s.VariantId != null)
            .GroupBy(s => s.VariantId!.Value)
            .Select(g => new { VariantId = g.Key, Available = g.Sum(x => x.QuantityOnHand - x.QuantityReserved) })
            .Where(x => x.Available > 0)
            .Select(x => x.VariantId)
            .ToListAsync(ct);

        return inStock.ToHashSet();
    }

    private async Task<List<Guid>> GetCategoryIdsAsync(Guid categoryId, bool includeDescendants, CancellationToken ct)
    {
        var categories = await _db.Categories
            .Where(c => !c.Isdeleted && c.IsActive)
            .ToListAsync(ct);

        var result = new HashSet<Guid> { categoryId };
        if (!includeDescendants) return result.ToList();

        var lookup = categories
            .Where(c => c.ParentCategoryId.HasValue)
            .GroupBy(c => c.ParentCategoryId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var queue = new Queue<Guid>();
        queue.Enqueue(categoryId);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!lookup.TryGetValue(current, out var children)) continue;
            foreach (var child in children)
                if (result.Add(child.Id)) queue.Enqueue(child.Id);
        }

        return result.ToList();
    }

    private static string SlugFromName(string name) =>
        name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("/", "-")
            .Replace("&", "and")
            .Replace("'", "")
            .Replace(",", "")
            .Trim('-');
}
