using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class StockLotMovementRepository : IStockLotMovementRepository
{
    private readonly AutoPartDbContext _dbContext;

    public StockLotMovementRepository(AutoPartDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<StockLotMovement>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockLotMovements
            .Where(x => !x.Isdeleted)
            .OrderByDescending(x => x.MovementDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<StockLotMovement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockLotMovements
            .FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task AddAsync(StockLotMovement entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        await _dbContext.StockLotMovements.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(StockLotMovement entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _dbContext.StockLotMovements.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.StockLotMovements
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity != null)
        {
            _dbContext.StockLotMovements.Remove(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockLotMovements
            .AnyAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task<IEnumerable<StockLotMovement>> GetByStockLotAsync(Guid stockLotId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockLotMovements
            .Where(x => x.StockLotId == stockLotId && !x.Isdeleted)
            .OrderByDescending(x => x.MovementDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<StockLotMovement>> GetByReferenceAsync(Guid referenceId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockLotMovements
            .Where(x => x.ReferenceId == referenceId && !x.Isdeleted)
            .OrderByDescending(x => x.MovementDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<StockLotMovement>> GetByMovementTypeAsync(string movementType, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockLotMovements
            .Where(x => x.MovementType == movementType && !x.Isdeleted)
            .OrderByDescending(x => x.MovementDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<StockLotMovement>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockLotMovements
            .Where(x => x.MovementDate >= startDate && x.MovementDate <= endDate && !x.Isdeleted)
            .OrderByDescending(x => x.MovementDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<StockLotMovement>> GetSalesMovementsAsync(Guid stockLotId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockLotMovements
            .Where(x => x.StockLotId == stockLotId && x.MovementType == "SALE" && !x.Isdeleted)
            .OrderByDescending(x => x.MovementDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<StockLotMovement>> GetByStockLotAndDateRangeAsync(Guid stockLotId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockLotMovements
            .Where(x => x.StockLotId == stockLotId && x.MovementDate >= startDate && x.MovementDate <= endDate && !x.Isdeleted)
            .OrderByDescending(x => x.MovementDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<StockLotMovement> items, int totalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.StockLotMovements
            .Where(x => !x.Isdeleted)
            .OrderByDescending(x => x.MovementDate);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
