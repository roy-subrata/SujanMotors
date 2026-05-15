using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class ProductVariantPriceHistoryRepository : IProductVariantPriceHistoryRepository
{
    private readonly AutoPartDbContext _dbContext;

    public ProductVariantPriceHistoryRepository(AutoPartDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<ProductVariantPriceHistory>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProductVariantPriceHistories
            .Where(x => !x.Isdeleted)
            .OrderByDescending(x => x.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductVariantPriceHistory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProductVariantPriceHistories
            .FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task<ProductVariantPriceHistory?> GetActiveVariantPriceAsync(
        Guid partId, Guid productVariantId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        return await _dbContext.ProductVariantPriceHistories
            .Where(x =>
                x.PartId == partId &&
                x.ProductVariantId == productVariantId &&
                x.StartDate <= today &&                          // must have started by today
                (x.EndDate == null || x.EndDate >= today) &&    // must not have ended before today
                !x.Isdeleted)
            .OrderByDescending(x => x.StartDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ProductVariantPriceHistory?> GetActiveProductPriceAsync(
        Guid partId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        return await _dbContext.ProductVariantPriceHistories
            .Where(x =>
                x.PartId == partId &&
                x.ProductVariantId == null &&
                x.StartDate <= today &&
                (x.EndDate == null || x.EndDate >= today) &&
                !x.Isdeleted)
            .OrderByDescending(x => x.StartDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ProductVariantPriceHistory?> ResolveActivePriceAsync(
        Guid partId, Guid? productVariantId, CancellationToken cancellationToken = default)
    {
        // 1. Variant-specific price
        if (productVariantId.HasValue)
        {
            var variantPrice = await GetActiveVariantPriceAsync(partId, productVariantId.Value, cancellationToken);
            if (variantPrice != null) return variantPrice;
        }

        // 2. Base product price
        return await GetActiveProductPriceAsync(partId, cancellationToken);
    }

    public async Task<ProductVariantPriceHistory?> GetPriceOnDateAsync(
        Guid partId, Guid? productVariantId, DateTime date, CancellationToken cancellationToken = default)
    {
        var targetDate = date.Date;

        // 1. Try variant-specific first
        if (productVariantId.HasValue)
        {
            var variantPrice = await _dbContext.ProductVariantPriceHistories
                .Where(x =>
                    x.PartId == partId &&
                    x.ProductVariantId == productVariantId &&
                    x.StartDate <= targetDate &&
                    (x.EndDate == null || x.EndDate >= targetDate) &&
                    !x.Isdeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (variantPrice != null) return variantPrice;
        }

        // 2. Base product price on that date
        return await _dbContext.ProductVariantPriceHistories
            .Where(x =>
                x.PartId == partId &&
                x.ProductVariantId == null &&
                x.StartDate <= targetDate &&
                (x.EndDate == null || x.EndDate >= targetDate) &&
                !x.Isdeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<ProductVariantPriceHistory>> GetByPartAsync(
        Guid partId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProductVariantPriceHistories
            .Where(x => x.PartId == partId && !x.Isdeleted)
            .OrderByDescending(x => x.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task SetNewPriceAsync(ProductVariantPriceHistory newPrice, CancellationToken cancellationToken = default)
    {
        // Close existing active price for same part+variant scope
        var current = newPrice.ProductVariantId.HasValue
            ? await GetActiveVariantPriceAsync(newPrice.PartId, newPrice.ProductVariantId.Value, cancellationToken)
            : await GetActiveProductPriceAsync(newPrice.PartId, cancellationToken);

        if (current != null)
        {
            var closeDate = newPrice.StartDate.AddDays(-1);
            if (closeDate < current.StartDate) closeDate = current.StartDate;
            current.Close(closeDate);
            _dbContext.ProductVariantPriceHistories.Update(current);
        }

        await _dbContext.ProductVariantPriceHistories.AddAsync(newPrice, cancellationToken);
        // SaveChangesAsync wraps all pending changes in a single DB transaction — no manual BeginTransaction needed.
        // Manual BeginTransaction would conflict with EnableRetryOnFailure on the DbContext.
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddAsync(ProductVariantPriceHistory entity, CancellationToken cancellationToken = default)
    {
        await _dbContext.ProductVariantPriceHistories.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ProductVariantPriceHistory entity, CancellationToken cancellationToken = default)
    {
        _dbContext.ProductVariantPriceHistories.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ProductVariantPriceHistories
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity != null)
        {
            entity.Isdeleted = true;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProductVariantPriceHistories
            .AnyAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }
}
