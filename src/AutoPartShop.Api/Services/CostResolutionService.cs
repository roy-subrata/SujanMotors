using AutoPartShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Services;

/// <summary>
/// Resolves the authoritative per-base-unit COST used to flag below-cost pricing: the FIFO
/// (oldest-received) sellable stock lot's cost, falling back to the variant's or product's
/// catalogue CostPrice. Shared by PricingController (POS pre-check) and SalesOrderController
/// (quick sale) so the two never diverge.
/// </summary>
public class CostResolutionService : ICostResolutionService
{
    private readonly AutoPartDbContext _dbContext;

    public CostResolutionService(AutoPartDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<decimal> ResolveCostPerBaseUnitAsync(
        Guid partId,
        Guid? productVariantId,
        CancellationToken cancellationToken = default)
    {
        var costs = await ResolveCostsPerBaseUnitAsync(
            new List<(Guid PartId, Guid? ProductVariantId)> { (partId, productVariantId) },
            cancellationToken);
        return costs.TryGetValue((partId, productVariantId), out var cost) ? cost : 0m;
    }

    public async Task<IDictionary<(Guid PartId, Guid? ProductVariantId), decimal>> ResolveCostsPerBaseUnitAsync(
        IList<(Guid PartId, Guid? ProductVariantId)> items,
        CancellationToken cancellationToken = default)
    {
        var pairs = items.Distinct().ToList();
        var result = new Dictionary<(Guid, Guid?), decimal>(pairs.Count);
        if (pairs.Count == 0)
            return result;

        var partIds = pairs.Select(p => p.PartId).Distinct().ToList();

        var lots = await _dbContext.StockLots
            .Where(l => partIds.Contains(l.PartId)
                && l.Status == Domain.Enums.StockLotStatus.AVAILABLE
                && l.IsActive
                && l.QuantityAvailableInBaseUnit > 0 && !l.Isdeleted)
            .OrderBy(l => l.ReceivingDate)
            .Select(l => new { l.PartId, l.VariantId, l.CostPriceInBaseUnit })
            .ToListAsync(cancellationToken);
        var fifoLotCostByKey = lots
            .GroupBy(l => (l.PartId, l.VariantId))
            .ToDictionary(g => g.Key, g => g.First().CostPriceInBaseUnit);

        var variantIds = pairs.Where(p => p.ProductVariantId.HasValue).Select(p => p.ProductVariantId!.Value).Distinct().ToList();
        var variantCostById = variantIds.Count == 0
            ? new Dictionary<Guid, decimal>()
            : await _dbContext.ProductVariants
                .AsNoTracking()
                .Where(v => variantIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id, v => v.CostPrice, cancellationToken);

        var partCostById = await _dbContext.Parts
            .AsNoTracking()
            .Where(p => partIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.CostPrice, cancellationToken);

        foreach (var pair in pairs)
        {
            decimal cost = 0m;
            if (fifoLotCostByKey.TryGetValue((pair.PartId, pair.ProductVariantId), out var lotCost) && lotCost > 0)
                cost = lotCost;
            else if (pair.ProductVariantId.HasValue && variantCostById.TryGetValue(pair.ProductVariantId.Value, out var variantCost) && variantCost > 0)
                cost = variantCost;
            else if (partCostById.TryGetValue(pair.PartId, out var partCost) && partCost > 0)
                cost = partCost;

            result[(pair.PartId, pair.ProductVariantId)] = cost;
        }

        return result;
    }
}
