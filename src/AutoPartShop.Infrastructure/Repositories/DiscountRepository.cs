using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class DiscountRepository : IDiscountRepository
{
    private readonly AutoPartDbContext _dbContext;

    public DiscountRepository(AutoPartDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<Discount>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Discounts
            .Where(x => !x.Isdeleted)
            .OrderByDescending(x => x.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Discount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Discounts
            .FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task<Discount?> GetByPromoCodeAsync(string promoCode, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Discounts
            .FirstOrDefaultAsync(x =>
                x.PromoCode == promoCode.ToUpper() &&
                x.IsActive &&
                !x.Isdeleted, cancellationToken);
    }

    public async Task<IEnumerable<Discount>> GetActiveDiscountsAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        return await _dbContext.Discounts
            .Where(x =>
                x.IsActive &&
                !x.Isdeleted &&
                x.StartDate <= today &&
                (!x.EndDate.HasValue || x.EndDate.Value >= today))
            .ToListAsync(cancellationToken);
    }

    public async Task<Discount?> GetVariantDiscountAsync(Guid partId, Guid productVariantId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        return await _dbContext.Discounts
            .Where(x =>
                x.PartId == partId &&
                x.ProductVariantId == productVariantId &&
                x.IsActive &&
                !x.Isdeleted &&
                x.StartDate <= today &&
                (!x.EndDate.HasValue || x.EndDate.Value >= today))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Discount?> GetProductDiscountAsync(Guid partId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        return await _dbContext.Discounts
            .Where(x =>
                x.PartId == partId &&
                x.ProductVariantId == null &&
                x.IsActive &&
                !x.Isdeleted &&
                x.StartDate <= today &&
                (!x.EndDate.HasValue || x.EndDate.Value >= today))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<Discount>> GetByPartAsync(Guid partId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Discounts
            .Where(x => x.PartId == partId && !x.Isdeleted)
            .OrderByDescending(x => x.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Discount entity, CancellationToken cancellationToken = default)
    {
        await _dbContext.Discounts.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Discount entity, CancellationToken cancellationToken = default)
    {
        _dbContext.Discounts.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Discounts
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity != null)
        {
            entity.Isdeleted = true;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Discounts
            .AnyAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }
}
