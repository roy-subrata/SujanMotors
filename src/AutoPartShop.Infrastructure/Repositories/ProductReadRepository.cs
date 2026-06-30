using AutoPartShop.Application.Parts;
using AutoPartShop.Application.Parts.Dtos;
using AutoPartShop.Domain.Entities;
using AutoPartsShop.Infrastructure.Extensions;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class ProductReadRepository(AutoPartDbContext _db) : IProductReadRepository
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
                RichDescription = x.Product.RichDescription,
                PartNumber = x.Product.PartNumber.Value,
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
                WidthCm = x.Product.WidthCm,
                HeightCm = x.Product.HeightCm,
                DepthCm = x.Product.DepthCm,
                TaxCode = x.Product.TaxCode,
                CreatedBy = x.Product.CreatedBy,
                ModifiedBy = x.Product.ModifiedBy,
                // Cosine distance ∈ [0,2]; convert to a 0..1 similarity (higher = closer).
                SimilarityScore = 1 - x.Distance
            })
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IEnumerable<ProductResponse> Parts, int TotalCount)> FindAllAsync(ProductQuery query, CancellationToken cancellationToken = default)
    {
        var term = query.Search.ToLower();

        if (query.FlattenVariants)
            return await FindAllFlattenedAsync(query, term, cancellationToken);

        var parts = _db.Parts
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Unit)
            .Include(p => p.BaseUnit)
            .Where(x => !x.Isdeleted && (query.IsActive == null || x.IsActive == query.IsActive) && (
             (EF.Functions.Like(x.Name, $"%{term}%") ||
             EF.Functions.Like(x.SKU, $"%{term}%") ||
             (x.LocalName != null && EF.Functions.Like(x.LocalName, $"%{term}%"))
            )));

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
                RichDescription = part.RichDescription,
                PartNumber = part.PartNumber.Value,
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
                WidthCm = part.WidthCm,
                HeightCm = part.HeightCm,
                DepthCm = part.DepthCm,
                TaxCode = part.TaxCode,
                CreatedBy = part.CreatedBy,
                ModifiedBy = part.ModifiedBy
            })
            .ToListAsync(cancellationToken);

        await ApplyLotCostAsync(items, cancellationToken);
        await ApplyVehicleFitAsync(items, cancellationToken);
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

        static decimal WeightedAvg(IEnumerable<(int Qty, decimal Cost)> rows)
        {
            long qty = 0; decimal value = 0;
            foreach (var r in rows) { qty += r.Qty; value += r.Qty * r.Cost; }
            return qty > 0 ? value / qty : 0;
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

    // Flattened view for transactional documents (PO, SO, GRN, POS):
    //   - Products WITHOUT active variants → returned as-is (base product)
    //   - Products WITH active variants    → each variant returned as its own line item
    // Search matches on product name, product SKU, variant name, or variant SKU.
    private async Task<(IEnumerable<ProductResponse> Parts, int TotalCount)> FindAllFlattenedAsync(
        ProductQuery query, string term, CancellationToken cancellationToken)
    {
        var baseItems = await _db.Parts
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Unit)
            .Include(p => p.BaseUnit)
            .Where(x => !x.Isdeleted && (query.IsActive == null || x.IsActive == query.IsActive)
                && !x.Variants.Any(v => v.IsActive && !v.Isdeleted)
                && (EF.Functions.Like(x.Name, $"%{term}%") || EF.Functions.Like(x.SKU, $"%{term}%")))
            .Select(part => new ProductResponse
            {
                Id = part.Id,
                Name = part.Name,
                DisplayName = part.Name,
                Description = part.Description,
                RichDescription = part.RichDescription,
                PartNumber = part.PartNumber.Value,
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
                WidthCm = part.WidthCm,
                HeightCm = part.HeightCm,
                DepthCm = part.DepthCm,
                TaxCode = part.TaxCode,
                CreatedBy = part.CreatedBy,
                ModifiedBy = part.ModifiedBy
            })
            .ToListAsync(cancellationToken);

        var variantItems = await _db.ProductVariants
            .Include(v => v.Part).ThenInclude(p => p!.Category)
            .Include(v => v.Part).ThenInclude(p => p!.Brand)
            .Include(v => v.Part).ThenInclude(p => p!.Unit)
            .Include(v => v.Part).ThenInclude(p => p!.BaseUnit)
            .Where(v => v.IsActive && !v.Isdeleted
                && v.Part != null && !v.Part.Isdeleted && (query.IsActive == null || v.Part.IsActive == query.IsActive)
                && (EF.Functions.Like(v.Name, $"%{term}%")
                    || (v.SKU != null && EF.Functions.Like(v.SKU, $"%{term}%"))
                    || EF.Functions.Like(v.Part.Name, $"%{term}%")
                    || EF.Functions.Like(v.Part.SKU, $"%{term}%")))
            .Select(v => new ProductResponse
            {
                Id = v.PartId,
                Name = v.Part!.Name,
                DisplayName = v.Name.StartsWith(v.Part.Name) ? v.Name : v.Part.Name + " - " + v.Name,
                Description = v.Part.Description,
                RichDescription = v.Part.RichDescription,
                PartNumber = v.Part.PartNumber.Value,
                SKU = v.Part.SKU,
                OemNumber = v.Part.OemNumber,
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
                WidthCm = v.WidthCm ?? v.Part.WidthCm,
                HeightCm = v.HeightCm ?? v.Part.HeightCm,
                DepthCm = v.DepthCm ?? v.Part.DepthCm,
                TaxCode = v.Part.TaxCode,
                CreatedBy = v.Part.CreatedBy,
                ModifiedBy = v.Part.ModifiedBy
            })
            .ToListAsync(cancellationToken);

        var allItems = baseItems.Concat(variantItems)
            .OrderBy(x => x.Name).ThenBy(x => x.VariantName)
            .ToList();

        var totalCount = allItems.Count;
        var paged = allItems
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        await ApplyLotCostAsync(paged, cancellationToken);
        await ApplyVehicleFitAsync(paged, cancellationToken);
        return (paged, totalCount);
    }

    public async Task<(IEnumerable<ProductPublicResponse> Parts, int TotalCount)> FindAllPublicAsync(ProductQuery query, CancellationToken cancellationToken = default)
    {
        var term = query.Search.ToLower();

        if (query.FlattenVariants)
            return await FindAllPublicFlattenedAsync(query, term, cancellationToken);

        var parts = _db.Parts
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Unit)
            .Include(p => p.BaseUnit)
            .Where(x => !x.Isdeleted && (query.IsActive == null || x.IsActive == query.IsActive) && (
             (EF.Functions.Like(x.Name, $"%{term}%") ||
             EF.Functions.Like(x.SKU, $"%{term}%")
            )));

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
            .Select(part => new ProductPublicResponse
            {
                Id = part.Id,
                Name = part.Name,
                DisplayName = part.Name,
                Description = part.Description,
                RichDescription = part.RichDescription,
                PartNumber = part.PartNumber.Value,
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
                SellingPrice = part.SellingPrice,
                EffectiveSellingPrice = part.SellingPrice,
                HasVariants = part.Variants.Any(v => v.IsActive && !v.Isdeleted),
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
                WidthCm = part.WidthCm,
                HeightCm = part.HeightCm,
                DepthCm = part.DepthCm,
                TaxCode = part.TaxCode
            })
            .ToListAsync(cancellationToken);

        await ApplyPublicVehicleFitAsync(items, cancellationToken);
        return (items, totalCount);
    }

    private async Task<(IEnumerable<ProductPublicResponse> Parts, int TotalCount)> FindAllPublicFlattenedAsync(
        ProductQuery query, string term, CancellationToken cancellationToken)
    {
        var baseItems = await _db.Parts
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Unit)
            .Include(p => p.BaseUnit)
            .Where(x => !x.Isdeleted && (query.IsActive == null || x.IsActive == query.IsActive)
                && !x.Variants.Any(v => v.IsActive && !v.Isdeleted)
                && (EF.Functions.Like(x.Name, $"%{term}%") || EF.Functions.Like(x.SKU, $"%{term}%")))
            .Select(part => new ProductPublicResponse
            {
                Id = part.Id,
                Name = part.Name,
                DisplayName = part.Name,
                Description = part.Description,
                RichDescription = part.RichDescription,
                PartNumber = part.PartNumber.Value,
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
                SellingPrice = part.SellingPrice,
                EffectiveSellingPrice = part.SellingPrice,
                HasVariants = false,
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
                WidthCm = part.WidthCm,
                HeightCm = part.HeightCm,
                DepthCm = part.DepthCm,
                TaxCode = part.TaxCode
            })
            .ToListAsync(cancellationToken);

        var variantItems = await _db.ProductVariants
            .Include(v => v.Part).ThenInclude(p => p!.Category)
            .Include(v => v.Part).ThenInclude(p => p!.Brand)
            .Include(v => v.Part).ThenInclude(p => p!.Unit)
            .Include(v => v.Part).ThenInclude(p => p!.BaseUnit)
            .Where(v => v.IsActive && !v.Isdeleted
                && v.Part != null && !v.Part.Isdeleted && (query.IsActive == null || v.Part.IsActive == query.IsActive)
                && (EF.Functions.Like(v.Name, $"%{term}%")
                    || (v.SKU != null && EF.Functions.Like(v.SKU, $"%{term}%"))
                    || EF.Functions.Like(v.Part.Name, $"%{term}%")
                    || EF.Functions.Like(v.Part.SKU, $"%{term}%")))
            .Select(v => new ProductPublicResponse
            {
                Id = v.PartId,
                Name = v.Part!.Name,
                DisplayName = v.Name.StartsWith(v.Part.Name) ? v.Name : v.Part.Name + " - " + v.Name,
                Description = v.Part.Description,
                RichDescription = v.Part.RichDescription,
                PartNumber = v.Part.PartNumber.Value,
                SKU = v.Part.SKU,
                OemNumber = v.Part.OemNumber,
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
                SellingPrice = v.Part.SellingPrice,
                EffectiveSellingPrice = v.SellingPrice > 0 ? v.SellingPrice : v.Part.SellingPrice,
                HasVariants = true,
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
                WidthCm = v.WidthCm ?? v.Part.WidthCm,
                HeightCm = v.HeightCm ?? v.Part.HeightCm,
                DepthCm = v.DepthCm ?? v.Part.DepthCm,
                TaxCode = v.Part.TaxCode
            })
            .ToListAsync(cancellationToken);

        var allItems = baseItems.Concat(variantItems)
            .OrderBy(x => x.Name).ThenBy(x => x.VariantName)
            .ToList();

        var totalCount = allItems.Count;
        var paged = allItems
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        await ApplyPublicVehicleFitAsync(paged, cancellationToken);
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

    private async Task ApplyPublicVehicleFitAsync(List<ProductPublicResponse> items, CancellationToken cancellationToken)
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
}
