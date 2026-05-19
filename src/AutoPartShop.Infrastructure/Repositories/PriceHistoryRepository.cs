using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class PriceHistoryRepository : IPriceHistoryRepository
{
    private readonly AutoPartDbContext _dbContext;

    public PriceHistoryRepository(AutoPartDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<PriceHistory>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.PriceHistories
            .Where(x => !x.Isdeleted)
            .OrderByDescending(x => x.EffectiveDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<PriceHistory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PriceHistories
            .FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task AddAsync(PriceHistory entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        await _dbContext.PriceHistories.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PriceHistory entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _dbContext.PriceHistories.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.PriceHistories
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity != null)
        {
            _dbContext.PriceHistories.Remove(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PriceHistories
            .AnyAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task<IEnumerable<PriceHistory>> GetByPartAsync(Guid partId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PriceHistories
            .Where(x => x.PartId == partId && !x.Isdeleted)
            .OrderByDescending(x => x.EffectiveDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<PriceHistory?> GetCurrentPriceAsync(Guid partId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PriceHistories
            .Where(x => x.PartId == partId && x.EffectiveDate <= DateTime.UtcNow && !x.Isdeleted)
            .OrderByDescending(x => x.EffectiveDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<PriceHistory>> GetByPartAndDateRangeAsync(Guid partId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PriceHistories
            .Where(x => x.PartId == partId && x.EffectiveDate >= startDate && x.EffectiveDate <= endDate && !x.Isdeleted)
            .OrderByDescending(x => x.EffectiveDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<PriceHistory?> GetLatestByPartAsync(Guid partId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PriceHistories
            .Where(x => x.PartId == partId && !x.Isdeleted)
            .OrderByDescending(x => x.EffectiveDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<decimal> GetPriceAtDateAsync(Guid partId, DateTime date, CancellationToken cancellationToken = default)
    {
        var priceHistory = await _dbContext.PriceHistories
            .Where(x => x.PartId == partId && x.EffectiveDate <= date && !x.Isdeleted)
            .OrderByDescending(x => x.EffectiveDate)
            .FirstOrDefaultAsync(cancellationToken);

        return priceHistory?.NewPrice ?? 0;
    }

    public async Task<IEnumerable<PriceHistory>> GetByReasonAsync(string reason, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PriceHistories
            .Where(x => x.Reason == reason && !x.Isdeleted)
            .OrderByDescending(x => x.EffectiveDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<PriceHistory> history, int totalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.PriceHistories
            .Where(x => !x.Isdeleted)
            .OrderByDescending(x => x.EffectiveDate);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
