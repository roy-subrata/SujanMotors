using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class StockMovementRepository : IStockMovementRepository
{
    private readonly AutoPartDbContext _dbContext;

    public StockMovementRepository(AutoPartDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<StockMovement>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<StockMovement>()
            .Include(x => x.StockLevel)
                .ThenInclude(sl => sl!.Part)
            .Include(x => x.StockLevel)
                .ThenInclude(sl => sl!.Warehouse)
            .Where(x => !x.Isdeleted)
            .OrderByDescending(x => x.MovementDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<StockMovement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<StockMovement>()
            .Include(x => x.StockLevel)
                .ThenInclude(sl => sl!.Part)
            .Include(x => x.StockLevel)
                .ThenInclude(sl => sl!.Warehouse)
            .FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task AddAsync(StockMovement entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        await _dbContext.Set<StockMovement>().AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(StockMovement entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _dbContext.Set<StockMovement>().Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Set<StockMovement>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity != null)
        {
            _dbContext.Set<StockMovement>().Remove(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<StockMovement>()
            .AnyAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task<IEnumerable<StockMovement>> GetByPartAsync(Guid partId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<StockMovement>()
            .Include(x => x.StockLevel)
            .Where(x => x.StockLevel != null && x.StockLevel.PartId == partId && !x.Isdeleted)
            .OrderByDescending(x => x.MovementDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<StockMovement>> GetByWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<StockMovement>()
            .Include(x => x.StockLevel)
            .Where(x => x.StockLevel != null && x.StockLevel.WarehouseId == warehouseId && !x.Isdeleted)
            .OrderByDescending(x => x.MovementDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<StockMovement>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<StockMovement>()
            .Where(x => x.MovementDate >= fromDate && x.MovementDate <= toDate && !x.Isdeleted)
            .OrderByDescending(x => x.MovementDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<StockMovement>> GetByTypeAsync(string movementType, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<StockMovement>()
            .Where(x => x.MovementType == movementType && !x.Isdeleted)
            .OrderByDescending(x => x.MovementDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<StockMovement>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<StockMovement>()
            .Where(x => !x.Isdeleted)
            .Where(x => string.IsNullOrEmpty(x.ApprovedBy) ? status == "PENDING" : status == "APPROVED")
            .OrderByDescending(x => x.MovementDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<StockMovement>> GetPendingApprovalsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<StockMovement>()
            .Where(x => string.IsNullOrEmpty(x.ApprovedBy) && !x.Isdeleted)
            .OrderBy(x => x.MovementDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<StockMovement>> GetByReferenceNumberAsync(string referenceNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(referenceNumber))
            return Enumerable.Empty<StockMovement>();

        var normalizedReference = referenceNumber.Trim();

        return await _dbContext.Set<StockMovement>()
            .Include(x => x.StockLevel)
                .ThenInclude(sl => sl!.Part)
            .Include(x => x.StockLevel)
                .ThenInclude(sl => sl!.Warehouse)
            .Where(x => x.ReferenceNumber == normalizedReference && !x.Isdeleted)
            .OrderByDescending(x => x.MovementDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<StockMovement> movements, int totalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<StockMovement>()
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
