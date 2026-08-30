using AutoPartShop.Application.Parts;
using AutoPartShop.Application.Parts.Dtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartsShop.Infrastructure.Extensions;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class ProductReadRepository(AutoPartDbContext _db, ICategoryRepository _categoryRepository) : IProductReadRepository
{
    // Semantic search: rank products by cosine distance between their stored embedding and the
    // query vector, entirely server-side (SQL Server 2025 VECTOR_DISTANCE), then paginate.
    public async Task<(IEnumerable<ProductResponse> Parts, int TotalCount)> SearchSemanticAsync(
        float[] queryVector, bool? isActive, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var sqlVector = new SqlVector<float>(queryVector);

        var ranked = _db.ProductEmbeddings
            .Where(e => !e.Isdeleted && e.Product != null && !e.Product.Isdeleted
                && (isActive == null || e.Product.IsActive == isActive))
            .Select(e => new
            {
                e.Product,
                Distance = EF.Functions.VectorDistance("cosine", e.Embedding, sqlVector)
            });

        var totalCount = await ranked.CountAsync(cancellationToken);

        var items = await ranked
            .OrderBy(x => x.Distance)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ProductResponse
            {
                Id = x.Product!.Id,
                Name = x.Product.Name,
                DisplayName = x.Product.Name,
                Description = x.Product.Description,
                PartNumber = x.Product.PartNumber != null ? x.Product.PartNumber.Value : "",
                SKU = x.Product.SKU,
                OemNumber = x.Product.OemNumber,
                LocalName = x.Product.LocalName,
                CategoryId = x.Product.CategoryId,
                CategoryName = x.Product.Category != null ? x.Product.Category.Name : string.Empty,
                BrandId = x.Product.BrandId,
                BrandName = x.Product.Brand != null ? x.Product.Brand.Name : null,
                BaseUnitId = x.Product.BaseUnitId,
                BaseUnitName = x.Product.BaseUnit != null ? x.Product.BaseUnit.Name : null,
                BaseUnitCode = x.Product.BaseUnit != null ? x.Product.BaseUnit.Symbol : null,
                UnitId = x.Product.UnitId,
                UnitName = x.Product.Unit != null ? x.Product.Unit.Name : null,
                CostPrice = x.Product.CostPrice,
                SellingPrice = x.Product.SellingPrice,
                EffectiveCostPrice = x.Product.CostPrice,
                EffectiveSellingPrice = x.Product.SellingPrice,
                HasVariants = x.Product.Variants.Any(v => v.IsActive && !v.Isdeleted),
                VariantCount = x.Product.Variants.Count(v => v.IsActive && !v.Isdeleted),
                IsVariant = false,
                MinimumStock = x.Product.MinimumStock,
                IsActive = x.Product.IsActive,
                HasWarranty = x.Product.HasWarranty,
                WarrantyPeriodMonths = x.Product.WarrantyPeriodMonths,
                WarrantyType = x.Product.WarrantyType,
                WarrantyTerms = x.Product.WarrantyTerms,
                WarrantyCertificateTemplate = x.Product.WarrantyCertificateTemplate,
                Barcode = x.Product.Barcode,
                Tags = x.Product.Tags,
                ProductType = x.Product.ProductType,
                IsPerishable = x.Product.IsPerishable,
                WeightKg = x.Product.WeightKg,
                TaxCode = x.Product.TaxCode,
                CreatedBy = x.Product.CreatedBy,
                ModifiedBy = x.Product.ModifiedBy,
                // Cosine distance ∈ [0,2]; convert to a 0..1 similarity (higher = closer).
                SimilarityScore = 1 - x.Distance
            })
            .ToListAsync(cancellationToken);

        // Same enrichment as FindAllAsync so search results carry the real inventory cost
        // and vehicle-fit summary instead of the stale catalog columns.
        await ApplyLotCostAsync(items, cancellationToken);
        await ApplyVehicleFitAsync(items, cancellationToken);
        await ApplyAttributeValuesAsync(items, cancellationToken);
        var stockMapSem = await GetStockTotalsAsync(items.Select(i => i.Id), cancellationToken);
        foreach (var it in items) it.TotalStock = stockMapSem.TryGetValue(it.Id, out var ss) ? ss : 0;
        return (items, totalCount);
    }

    public async Task<(IEnumerable<ProductResponse> Parts, int TotalCount)> FindAllAsync(ProductQuery query, CancellationToken cancellationToken = default)
    {
        var term = query.Search.ToLower();
        var categoryIds = await ResolveCategoryIdsAsync(query.CategoryId, cancellationToken);
        var attributeGroups = await ResolveAttributeOptionGroupsAsync(query.AttributeOptionIds, cancellationToken);

        if (query.FlattenVariants)
            return await FindAllFlattenedAsync(query, term, categoryIds, attributeGroups, cancellationToken);

        var parts = _db.Parts
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Unit)
            .Include(p => p.BaseUnit)
            .Where(x => !x.Isdeleted)
            .Where(x => query.IsActive == null || x.IsActive == query.IsActive)
            .Where(x => categoryIds == null || categoryIds.Contains(x.CategoryId))
            .Where(x => query.VehicleIds == null || !query.VehicleIds.Any()
                || x.VehicleCompatibilities.Any(vc =>
                    !vc.Isdeleted && vc.IsCompatible && query.VehicleIds.Contains(vc.VehicleId)));

        // Each word must match SOMEWHERE (name/sku/attribute, etc.) — a different field per word is
        // fine, so "battery white" finds a product named "Battery" with a White variant.
        foreach (var raw in SplitTerms(term))
        {
            var t = EscapeLikeTerm(raw);
            parts = parts.Where(x =>
                EF.Functions.Like(x.Name, $"%{t}%") ||
                EF.Functions.Like(x.SKU, $"%{t}%") ||
                (x.LocalName != null && EF.Functions.Like(x.LocalName, $"%{t}%")) ||
                (x.PartNumber != null && EF.Functions.Like(x.PartNumber.Value, $"%{t}%")) ||
                (x.OemNumber != null && EF.Functions.Like(x.OemNumber, $"%{t}%")) ||
                x.AttributeValues.Any(av => EF.Functions.Like(av.ValueText, $"%{t}%") ||
                   (av.Option != null && EF.Functions.Like(av.Option.Value, $"%{t}%"))) ||
                x.Variants.Any(v => v.Attributes.Any(av => EF.Functions.Like(av.ValueText, $"%{t}%") ||
                   (av.Option != null && EF.Functions.Like(av.Option.Value, $"%{t}%")))));
        }

        // Faceted filter: a product must match ALL selected attributes (AND across groups), matching
        // ANY of that attribute's selected options (OR within the group). A value at either the
        // product level or on any of its variants counts — same "either level" rule as search above.
        foreach (var (attributeId, optionIds) in attributeGroups)
        {
            parts = parts.Where(x =>
                x.AttributeValues.Any(av => av.AttributeId == attributeId && av.OptionId != null && optionIds.Contains(av.OptionId.Value)) ||
                x.Variants.Any(v => v.Attributes.Any(av => av.AttributeId == attributeId && av.OptionId != null && optionIds.Contains(av.OptionId.Value))));
        }

        if (query.LowStockOnly)
        {
            // Same rule as the reorder alerts: at/below an opted-in reorder point.
            parts = parts.Where(p => _db.StockLevels.Any(sl =>
                !sl.Isdeleted && sl.IsActive && sl.PartId == p.Id
                && sl.ReorderLevel > 0
                && (sl.QuantityOnHand - sl.QuantityReserved) <= sl.ReorderLevel));
        }

        if (query.Sorts != null && query.Sorts.Any())
        {
            var sorts = query.Sorts.Select(x => (x.Field, x.Direction == "asc" ? true : false)).ToArray();
            parts = parts.OrderByMultiple(sorts);
        }
        else
        {
            parts = parts.OrderByDescending(x => x.CreatedDate);
        }

        var totalCount = await parts.CountAsync(cancellationToken);
        var items = await parts
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(part => new ProductResponse
            {
                Id = part.Id,
                Name = part.Name,
                DisplayName = part.Name,
                Description = part.Description,
                PartNumber = part.PartNumber != null ? part.PartNumber.Value : "",
                SKU = part.SKU,
                OemNumber = part.OemNumber,
                LocalName = part.LocalName,
                CategoryId = part.CategoryId,
                CategoryName = part.Category != null ? part.Category.Name : string.Empty,
                BrandId = part.BrandId,
                BrandName = part.Brand != null ? part.Brand.Name : null,
                BaseUnitId = part.BaseUnitId,
                BaseUnitName = part.BaseUnit != null ? part.BaseUnit.Name : null,
                BaseUnitCode = part.BaseUnit != null ? part.BaseUnit.Symbol : null,
                UnitId = part.UnitId,
                UnitName = part.Unit != null ? part.Unit.Name : null,
                CostPrice = part.CostPrice,
                SellingPrice = part.SellingPrice,
                EffectiveCostPrice = part.CostPrice,
                EffectiveSellingPrice = part.SellingPrice,
                HasVariants = part.Variants.Any(v => v.IsActive && !v.Isdeleted),
                VariantCount = part.Variants.Count(v => v.IsActive && !v.Isdeleted),
                IsVariant = false,
                MinimumStock = part.MinimumStock,
                IsActive = part.IsActive,
                HasWarranty = part.HasWarranty,
                WarrantyPeriodMonths = part.WarrantyPeriodMonths,
                WarrantyType = part.WarrantyType,
                WarrantyTerms = part.WarrantyTerms,
                WarrantyCertificateTemplate = part.WarrantyCertificateTemplate,
                Barcode = part.Barcode,
                Tags = part.Tags,
                ProductType = part.ProductType,
                IsPerishable = part.IsPerishable,
                WeightKg = part.WeightKg,
                TaxCode = part.TaxCode,
                CreatedBy = part.CreatedBy,
                ModifiedBy = part.ModifiedBy
            })
            .ToListAsync(cancellationToken);

        await ApplyLotCostAsync(items, cancellationToken);
        await ApplyVehicleFitAsync(items, cancellationToken);
        await ApplyAttributeValuesAsync(items, cancellationToken);
        await ApplyMatchedAttributeHintAsync(items, term, cancellationToken);
        var stockMapAll = await GetStockTotalsAsync(items.Select(i => i.Id), cancellationToken);
        foreach (var it in items) it.TotalStock = stockMapAll.TryGetValue(it.Id, out var sa) ? sa : 0;
        return (items, totalCount);
    }

    /// <summary>
    /// Overwrites each item's <c>CostPrice</c>/<c>EffectiveCostPrice</c> with the weighted-average
    /// cost of its on-hand purchase lots — the actual inventory cost. Product rows average across all
    /// of the part's on-hand lots; variant rows average only that variant's lots. Items with no
    /// on-hand lots get 0 (no purchase yet → no cost). One batched query for the whole page.
    /// </summary>
    private async Task ApplyLotCostAsync(List<ProductResponse> items, CancellationToken cancellationToken)
    {
        if (items.Count == 0) return;

        var partIds = items.Select(i => i.Id).Distinct().ToList();
        var lots = await _db.StockLots
            .Where(l => !l.Isdeleted && l.QuantityAvailable > 0 && partIds.Contains(l.PartId))
            .Select(l => new { l.PartId, l.VariantId, l.QuantityAvailable, l.CostPrice })
            .ToListAsync(cancellationToken);

        if (lots.Count == 0)
        {
            foreach (var it in items) { it.CostPrice = 0; it.EffectiveCostPrice = 0; }
            return;
        }

        var byPart = lots
            .GroupBy(l => l.PartId)
            .ToDictionary(g => g.Key, g => WeightedAvg(g.Select(x => (x.QuantityAvailable, x.CostPrice))));
        var byVariant = lots
            .Where(l => l.VariantId != null)
            .GroupBy(l => (l.PartId, VariantId: l.VariantId!.Value))
            .ToDictionary(g => g.Key, g => WeightedAvg(g.Select(x => (x.QuantityAvailable, x.CostPrice))));

        foreach (var it in items)
        {
            decimal cost = it.IsVariant && it.VariantId.HasValue
                ? (byVariant.TryGetValue((it.Id, it.VariantId.Value), out var vc) ? vc : 0)
                : (byPart.TryGetValue(it.Id, out var pc) ? pc : 0);
            it.CostPrice = cost;
            it.EffectiveCostPrice = cost;
        }
    }

    /// <inheritdoc />
    public async Task<(decimal PartCost, IReadOnlyDictionary<Guid, decimal> VariantCosts)> GetWeightedLotCostsAsync(
        Guid partId, CancellationToken cancellationToken = default)
    {
        var lots = await _db.StockLots
            .Where(l => !l.Isdeleted && l.QuantityAvailable > 0 && l.PartId == partId)
            .Select(l => new { l.VariantId, l.QuantityAvailable, l.CostPrice })
            .ToListAsync(cancellationToken);

        var variantCosts = lots
            .Where(l => l.VariantId != null)
            .GroupBy(l => l.VariantId!.Value)
            .ToDictionary(g => g.Key, g => WeightedAvg(g.Select(x => (x.QuantityAvailable, x.CostPrice))));

        return (WeightedAvg(lots.Select(x => (x.QuantityAvailable, x.CostPrice))), variantCosts);
    }

    private static decimal WeightedAvg(IEnumerable<(int Qty, decimal Cost)> rows)
    {
        long qty = 0; decimal value = 0;
        foreach (var r in rows) { qty += r.Qty; value += r.Qty * r.Cost; }
        return qty > 0 ? value / qty : 0;
    }

    /// <summary>
    /// Category id(s) a CategoryId filter should match against: the category itself plus all of its
    /// descendants, so filtering by a parent (e.g. "Wheels &amp; Tires") also returns products filed
    /// under its children (e.g. "Tires"). Null means "no category filter" (matches everything).
    /// </summary>
    private async Task<HashSet<Guid>?> ResolveCategoryIdsAsync(Guid? categoryId, CancellationToken cancellationToken)
    {
        if (categoryId is null) return null;

        var descendants = await _categoryRepository.GetAllDescendantsAsync(categoryId.Value, cancellationToken);
        return new[] { categoryId.Value }.Concat(descendants.Select(c => c.Id)).ToHashSet();
    }

    /// <summary>
    /// Groups selected attribute-option ids by their owning attribute, so callers can AND across
    /// distinct attributes while ORing within each attribute's selected options.
    /// </summary>
    private async Task<List<(Guid AttributeId, List<Guid> OptionIds)>> ResolveAttributeOptionGroupsAsync(
        IReadOnlyCollection<Guid>? optionIds, CancellationToken cancellationToken)
    {
        if (optionIds is null || optionIds.Count == 0) return [];

        var rows = await _db.ProductAttributeOptions
            .Where(o => optionIds.Contains(o.Id))
            .Select(o => new { o.Id, o.AttributeId })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(o => o.AttributeId)
            .Select(g => (g.Key, g.Select(x => x.Id).ToList()))
            .ToList();
    }

    // Flattened view for transactional documents (PO, SO, GRN, POS):
    //   - Products WITHOUT active variants → returned as-is (base product)
    //   - Products WITH active variants    → each variant returned as its own line item
    // Search matches on product name, product SKU, variant name, or variant SKU.
    private async Task<(IEnumerable<ProductResponse> Parts, int TotalCount)> FindAllFlattenedAsync(
        ProductQuery query, string term, HashSet<Guid>? categoryIds,
        List<(Guid AttributeId, List<Guid> OptionIds)> attributeGroups, CancellationToken cancellationToken)
    {
        var baseQuery = _db.Parts
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Unit)
            .Include(p => p.BaseUnit)
            .Where(x => !x.Isdeleted)
            .Where(x => query.IsActive == null || x.IsActive == query.IsActive)
            .Where(x => categoryIds == null || categoryIds.Contains(x.CategoryId))
            .Where(x => query.VehicleIds == null || !query.VehicleIds.Any()
                || x.VehicleCompatibilities.Any(vc =>
                    !vc.Isdeleted && vc.IsCompatible && query.VehicleIds.Contains(vc.VehicleId)))
            .Where(x => !x.Variants.Any(v => v.IsActive && !v.Isdeleted));

        // Each word must match SOMEWHERE (name/sku/attribute) — see FindAllAsync for the same pattern.
        foreach (var raw in SplitTerms(term))
        {
            var t = EscapeLikeTerm(raw);
            baseQuery = baseQuery.Where(x => EF.Functions.Like(x.Name, $"%{t}%") || EF.Functions.Like(x.SKU, $"%{t}%") ||
                (x.LocalName != null && EF.Functions.Like(x.LocalName, $"%{t}%")) ||
                (x.PartNumber != null && EF.Functions.Like(x.PartNumber.Value, $"%{t}%")) ||
                (x.OemNumber != null && EF.Functions.Like(x.OemNumber, $"%{t}%")) ||
                x.AttributeValues.Any(av => EF.Functions.Like(av.ValueText, $"%{t}%") ||
                    (av.Option != null && EF.Functions.Like(av.Option.Value, $"%{t}%"))));
        }

        // Faceted filter (product-level only — this branch only has products with no active
        // variants, so there is no variant-level value to fall back to).
        foreach (var (attributeId, optionIds) in attributeGroups)
        {
            baseQuery = baseQuery.Where(x =>
                x.AttributeValues.Any(av => av.AttributeId == attributeId && av.OptionId != null && optionIds.Contains(av.OptionId.Value)));
        }

        var baseItems = await baseQuery
            .Select(part => new ProductResponse
            {
                Id = part.Id,
                Name = part.Name,
                DisplayName = part.Name,
                Description = part.Description,
                PartNumber = part.PartNumber != null ? part.PartNumber.Value : "",
                SKU = part.SKU,
                OemNumber = part.OemNumber,
                LocalName = part.LocalName,
                CategoryId = part.CategoryId,
                CategoryName = part.Category != null ? part.Category.Name : string.Empty,
                BrandId = part.BrandId,
                BrandName = part.Brand != null ? part.Brand.Name : null,
                BaseUnitId = part.BaseUnitId,
                BaseUnitName = part.BaseUnit != null ? part.BaseUnit.Name : null,
                BaseUnitCode = part.BaseUnit != null ? part.BaseUnit.Symbol : null,
                UnitId = part.UnitId,
                UnitName = part.Unit != null ? part.Unit.Name : null,
                CostPrice = part.CostPrice,
                SellingPrice = part.SellingPrice,
                EffectiveCostPrice = part.CostPrice,
                EffectiveSellingPrice = part.SellingPrice,
                HasVariants = false,
                VariantCount = 0,
                IsVariant = false,
                MinimumStock = part.MinimumStock,
                IsActive = part.IsActive,
                HasWarranty = part.HasWarranty,
                WarrantyPeriodMonths = part.WarrantyPeriodMonths,
                WarrantyType = part.WarrantyType,
                WarrantyTerms = part.WarrantyTerms,
                WarrantyCertificateTemplate = part.WarrantyCertificateTemplate,
                Barcode = part.Barcode,
                Tags = part.Tags,
                ProductType = part.ProductType,
                IsPerishable = part.IsPerishable,
                WeightKg = part.WeightKg,
                TaxCode = part.TaxCode,
                CreatedBy = part.CreatedBy,
                ModifiedBy = part.ModifiedBy
            })
            .ToListAsync(cancellationToken);

        // Variant branch — everything (search term, IsActive, CategoryId) is filtered in SQL and
        // the projection is composed inline, so only matching rows are materialized. Navigations
        // referenced by the projection become joins automatically; no Includes needed.
        var variantQuery = _db.ProductVariants
            .Where(v => v.IsActive && !v.Isdeleted && v.Part != null && !v.Part.Isdeleted)
            .Where(v => query.IsActive == null || v.Part!.IsActive == query.IsActive)
            .Where(v => categoryIds == null || categoryIds.Contains(v.Part!.CategoryId));

        // NOTE: intentionally not matching on v.PartNumber here. Member access on the PartNumber
        // value-converted type (v.PartNumber.Value) cannot be translated by EF Core once this
        // query's Join between ProductVariant and Product is in scope — every shape tried (direct
        // access, EF.Property, a correlated subquery) throws server-side. The equivalent access on
        // the root Parts query below works fine, so this is scoped specifically to the variant/Join
        // path. Until that's resolved upstream, variant part numbers are excluded from search;
        // product name/SKU/OEM and the parent product's name/SKU still match.
        // Each word must match SOMEWHERE — see FindAllAsync for the same pattern.
        foreach (var raw in SplitTerms(term))
        {
            var t = EscapeLikeTerm(raw);
            variantQuery = variantQuery.Where(v =>
                EF.Functions.Like(v.Name, $"%{t}%") ||
                (v.SKU != null && EF.Functions.Like(v.SKU, $"%{t}%")) ||
                (v.OemNumber != null && EF.Functions.Like(v.OemNumber, $"%{t}%")) ||
                EF.Functions.Like(v.Part!.Name, $"%{t}%") ||
                EF.Functions.Like(v.Part.SKU, $"%{t}%") ||
                v.Attributes.Any(av => EF.Functions.Like(av.ValueText, $"%{t}%") ||
                    (av.Option != null && EF.Functions.Like(av.Option.Value, $"%{t}%"))) ||
                v.Part.AttributeValues.Any(av => EF.Functions.Like(av.ValueText, $"%{t}%") ||
                    (av.Option != null && EF.Functions.Like(av.Option.Value, $"%{t}%"))));
        }

        // Faceted filter: a value on the variant itself OR its parent product counts — same
        // "either level" rule as the search predicate above.
        foreach (var (attributeId, optionIds) in attributeGroups)
        {
            variantQuery = variantQuery.Where(v =>
                v.Attributes.Any(av => av.AttributeId == attributeId && av.OptionId != null && optionIds.Contains(av.OptionId.Value)) ||
                v.Part!.AttributeValues.Any(av => av.AttributeId == attributeId && av.OptionId != null && optionIds.Contains(av.OptionId.Value)));
        }

        var variantItems = await variantQuery
            .Select(v => new ProductResponse
            {
                Id = v.PartId,
                Name = v.Part!.Name,
                DisplayName = v.Name.StartsWith(v.Part.Name) ? v.Name : v.Part.Name + " - " + v.Name,
                Description = v.Part.Description,
                PartNumber = v.PartNumber != null ? v.PartNumber.Value
                    : (v.Part.PartNumber != null ? v.Part.PartNumber.Value : ""),
                SKU = v.Part.SKU,
                OemNumber = v.OemNumber ?? v.Part.OemNumber,
                LocalName = v.Part.LocalName,
                CategoryId = v.Part.CategoryId,
                CategoryName = v.Part.Category != null ? v.Part.Category.Name : string.Empty,
                BrandId = v.Part.BrandId,
                BrandName = v.Part.Brand != null ? v.Part.Brand.Name : null,
                BaseUnitId = v.Part.BaseUnitId,
                BaseUnitName = v.Part.BaseUnit != null ? v.Part.BaseUnit.Name : null,
                BaseUnitCode = v.Part.BaseUnit != null ? v.Part.BaseUnit.Symbol : null,
                UnitId = v.Part.UnitId,
                UnitName = v.Part.Unit != null ? v.Part.Unit.Name : null,
                CostPrice = v.Part.CostPrice,
                SellingPrice = v.Part.SellingPrice,
                EffectiveCostPrice = v.CostPrice,
                EffectiveSellingPrice = v.SellingPrice > 0 ? v.SellingPrice : v.Part.SellingPrice,
                HasVariants = true,
                VariantCount = 1,
                IsVariant = true,
                VariantId = v.Id,
                VariantName = v.Name,
                VariantCode = v.Code,
                VariantSKU = v.SKU,
                VariantBarcode = v.Barcode,
                MinimumStock = v.Part.MinimumStock,
                IsActive = v.Part.IsActive,
                HasWarranty = v.HasWarrantyOverride ?? v.Part.HasWarranty,
                WarrantyPeriodMonths = v.HasWarrantyOverride.HasValue ? v.WarrantyPeriodMonthsOverride : v.Part.WarrantyPeriodMonths,
                WarrantyType = v.HasWarrantyOverride.HasValue ? v.WarrantyTypeOverride : v.Part.WarrantyType,
                WarrantyTerms = v.Part.WarrantyTerms,
                WarrantyCertificateTemplate = v.Part.WarrantyCertificateTemplate,
                Barcode = v.Barcode ?? v.Part.Barcode,
                Tags = v.Part.Tags,
                ProductType = v.Part.ProductType,
                IsPerishable = v.Part.IsPerishable,
                WeightKg = v.WeightKg ?? v.Part.WeightKg,
                TaxCode = v.Part.TaxCode,
                CreatedBy = v.Part.CreatedBy,
                ModifiedBy = v.Part.ModifiedBy
            })
            .ToListAsync(cancellationToken);

        var allItems = baseItems.Concat(variantItems).ToList();

        // Honor the caller's sort when one is supplied (same fields as the non-flattened path);
        // default ordering keeps name/variant grouping for pickers that don't request a sort.
        var sort = query.Sorts?.FirstOrDefault();
        allItems = sort?.Field?.ToLowerInvariant() switch
        {
            "sellingprice" => sort.Direction == "desc"
                ? allItems.OrderByDescending(x => x.EffectiveSellingPrice).ThenBy(x => x.VariantName).ToList()
                : allItems.OrderBy(x => x.EffectiveSellingPrice).ThenBy(x => x.VariantName).ToList(),
            "name" => sort.Direction == "desc"
                ? allItems.OrderByDescending(x => x.Name).ThenByDescending(x => x.VariantName).ToList()
                : allItems.OrderBy(x => x.Name).ThenBy(x => x.VariantName).ToList(),
            _ => allItems.OrderBy(x => x.Name).ThenBy(x => x.VariantName).ToList()
        };

        var totalCount = allItems.Count;
        var paged = allItems
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        await ApplyLotCostAsync(paged, cancellationToken);
        await ApplyVehicleFitAsync(paged, cancellationToken);
        await ApplyAttributeValuesAsync(paged, cancellationToken);
        await ApplyMatchedAttributeHintAsync(paged, term, cancellationToken);
        var stockMapFlat = await GetStockTotalsAsync(paged.Select(i => i.Id), cancellationToken);
        foreach (var it in paged) it.TotalStock = stockMapFlat.TryGetValue(it.Id, out var sf) ? sf : 0;
        return (paged, totalCount);
    }

    private async Task ApplyVehicleFitAsync(List<ProductResponse> items, CancellationToken cancellationToken)
    {
        if (items.Count == 0) return;

        var partIds = items.Select(i => i.Id).Distinct().ToList();
        var compatibilities = await _db.PartVehicleCompatibilities
            .Include(vc => vc.Vehicle)
            .Where(vc => !vc.Isdeleted && vc.IsCompatible && partIds.Contains(vc.PartId) && vc.Vehicle != null)
            .Select(vc => new { vc.PartId, Make = vc.Vehicle!.Make, Model = vc.Vehicle.Model, Year = vc.Vehicle.Year })
            .ToListAsync(cancellationToken);

        var byPart = compatibilities
            .GroupBy(c => c.PartId)
            .ToDictionary(g => g.Key, g => g.OrderBy(c => c.Make).ToList());

        foreach (var item in items)
        {
            if (!byPart.TryGetValue(item.Id, out var vehicles) || vehicles.Count == 0)
                continue;

            var labels = vehicles.Take(2).Select(v => $"{v.Make} {v.Model} {v.Year}");
            var summary = string.Join(", ", labels);
            if (vehicles.Count > 2)
                summary += $" +{vehicles.Count - 2}";
            item.VehicleFit = summary;
        }
    }

    /// <summary>
    /// Populates each item's <c>AttributeValues</c> with the product's product-scoped EAV attribute
    /// values (e.g. Material). Batched by product id, same enrichment pattern as
    /// <see cref="ApplyLotCostAsync"/>/<see cref="ApplyVehicleFitAsync"/>.
    /// </summary>
    private async Task ApplyAttributeValuesAsync(List<ProductResponse> items, CancellationToken cancellationToken)
    {
        if (items.Count == 0) return;

        var partIds = items.Select(i => i.Id).Distinct().ToList();
        var values = await _db.ProductAttributeValues
            .Where(v => !v.Isdeleted && partIds.Contains(v.ProductId))
            .Include(v => v.Attribute)
            .Include(v => v.Option)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (values.Count == 0) return;

        var byPart = values.GroupBy(v => v.ProductId).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var item in items)
        {
            if (!byPart.TryGetValue(item.Id, out var av)) continue;
            item.AttributeValues = av.Select(a => new ProductAttributeValueSummary
            {
                AttributeId = a.AttributeId,
                AttributeName = a.Attribute?.Name ?? string.Empty,
                DataType = a.Attribute?.DataType,
                OptionId = a.OptionId,
                OptionValue = a.Option?.Value,
                ValueText = a.ValueText,
                ValueNumber = a.ValueNumber,
                ValueBool = a.ValueBool
            }).ToList();
        }
    }

    /// <summary>
    /// Populates <c>MatchedAttributeLabel</c>/<c>MatchedVariantCount</c> for rows whose search-term
    /// match came from an attribute value rather than a visible field. Rows that already match on
    /// Name/SKU/LocalName/PartNumber/OemNumber are left alone (the match is self-explanatory).
    /// Checks the already-populated product-scope <c>AttributeValues</c> first, then falls back to a
    /// single batched variant-level query, same enrichment pattern as <see cref="ApplyLotCostAsync"/>/
    /// <see cref="ApplyVehicleFitAsync"/>. Must run after <see cref="ApplyAttributeValuesAsync"/>.
    /// </summary>
    private async Task ApplyMatchedAttributeHintAsync(List<ProductResponse> items, string term, CancellationToken cancellationToken)
    {
        if (items.Count == 0 || string.IsNullOrWhiteSpace(term)) return;

        var words = SplitTerms(term);
        if (words.Length == 0) return;

        // Per item, only the words NOT already explained by a visible field (Name/SKU/etc.) need an
        // attribute-driven hint — e.g. for "battery white" on a product named "Battery", "battery" is
        // already obvious from the name, so only "white" needs explaining.
        var missingByItem = new Dictionary<ProductResponse, List<string>>();
        foreach (var item in items)
        {
            var missing = words.Where(w => !MatchesVisibleFields(item, w)).ToList();
            if (missing.Count > 0) missingByItem[item] = missing;
        }
        if (missingByItem.Count == 0) return;

        var stillUnmatched = new List<ProductResponse>();
        foreach (var (item, missingWords) in missingByItem)
        {
            var hit = item.AttributeValues.FirstOrDefault(a =>
                missingWords.Any(w => Contains(a.ValueText, w) || Contains(a.OptionValue, w)));

            if (hit != null)
                item.MatchedAttributeLabel = $"{hit.AttributeName}: {hit.OptionValue ?? hit.ValueText}";
            else
                stillUnmatched.Add(item);
        }

        if (stillUnmatched.Count == 0) return;

        var partIds = stillUnmatched.Select(i => i.Id).Distinct().ToList();
        var variantValues = await _db.VariantAttributeValues
            .Where(av => !av.Isdeleted && av.Variant != null && !av.Variant.Isdeleted
                && partIds.Contains(av.Variant.PartId))
            .Include(av => av.Attribute)
            .Include(av => av.Option)
            .Include(av => av.Variant)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (variantValues.Count == 0) return;

        var byPart = variantValues.GroupBy(av => av.Variant!.PartId).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var item in stillUnmatched)
        {
            if (!byPart.TryGetValue(item.Id, out var candidates)) continue;
            var missingWords = missingByItem[item];

            var matches = candidates
                .Where(av => missingWords.Any(w => Contains(av.ValueText, w) || (av.Option != null && Contains(av.Option.Value, w))))
                .ToList();
            if (matches.Count == 0) continue;

            var first = matches[0];
            item.MatchedAttributeLabel = $"{first.Attribute?.Name}: {first.Option?.Value ?? first.ValueText}";

            var distinctVariantCount = matches.Select(m => m.VariantId).Distinct().Count();
            if (distinctVariantCount > 1)
                item.MatchedVariantCount = distinctVariantCount;
        }
    }

    private static bool MatchesVisibleFields(ProductResponse item, string term) =>
        Contains(item.Name, term) || Contains(item.SKU, term) || Contains(item.LocalName, term) ||
        Contains(item.PartNumber, term) || Contains(item.OemNumber, term);

    private static bool Contains(string? value, string term) =>
        !string.IsNullOrEmpty(value) && value.Contains(term, StringComparison.OrdinalIgnoreCase);

    /// <summary>Splits a search box value into lowercased words — each word must match SOMEWHERE
    /// (a possibly different field per word), rather than the whole phrase matching one field.</summary>
    private static string[] SplitTerms(string term) =>
        term.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.ToLowerInvariant())
            .ToArray();

    /// <summary>Escapes SQL Server LIKE wildcard metacharacters (%, _, [) so a search word containing
    /// one of them (e.g. a SKU like "50%-OFF" or "A_1") is matched literally, not as a wildcard.
    /// Bracket-style escaping needs no ESCAPE clause in T-SQL. Order matters: '[' must be escaped
    /// first, or the brackets introduced while escaping '%'/'_' would themselves get re-escaped.</summary>
    private static string EscapeLikeTerm(string term) =>
        term.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");

    private async Task<Dictionary<Guid, int>> GetStockTotalsAsync(
        IEnumerable<Guid> partIds, CancellationToken cancellationToken)
    {
        var ids = partIds.Distinct().ToList();
        if (ids.Count == 0) return [];

        var rows = await _db.StockLevels
            .Where(s => !s.Isdeleted && ids.Contains(s.PartId))
            .GroupBy(s => s.PartId)
            .Select(g => new { PartId = g.Key, Total = g.Sum(s => s.QuantityOnHand - s.QuantityReserved) })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.PartId, r => r.Total < 0 ? 0 : r.Total);
    }
}
