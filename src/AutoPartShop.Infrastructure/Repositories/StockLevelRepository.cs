using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class StockLevelRepository : IStockLevelRepository
{
    private readonly AutoPartDbContext _dbContext;

    public StockLevelRepository(AutoPartDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<StockLevel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<StockLevel>()
            .Where(x => !x.Isdeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<StockLevel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<StockLevel>()
            .Include(x => x.Part)
            .Include(x => x.Variant)
            .Include(x => x.Warehouse)
            .FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task AddAsync(StockLevel entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        await _dbContext.Set<StockLevel>().AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(StockLevel entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _dbContext.Set<StockLevel>().Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Set<StockLevel>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity != null)
        {
            _dbContext.Set<StockLevel>().Remove(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<StockLevel>()
            .AnyAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task<StockLevel?> GetByPartAndWarehouseAsync(Guid partId, Guid warehouseId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<StockLevel>()
            .Include(x => x.Part)
            .Include(x => x.Variant)
            .Include(x => x.Warehouse)
            .FirstOrDefaultAsync(x => x.PartId == partId && x.WarehouseId == warehouseId && !x.Isdeleted, cancellationToken);
    }

    public async Task<StockLevel?> GetByPartVariantAndWarehouseAsync(Guid partId, Guid? variantId, Guid warehouseId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<StockLevel>()
            .Include(x => x.Part)
            .Include(x => x.Variant)
            .Include(x => x.Warehouse)
            .FirstOrDefaultAsync(x => x.PartId == partId && x.VariantId == variantId && x.WarehouseId == warehouseId && !x.Isdeleted, cancellationToken);
    }

    public async Task<IEnumerable<StockLevel>> GetByPartAndVariantAsync(Guid partId, Guid? variantId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<StockLevel>()
            .Where(x => x.PartId == partId && x.VariantId == variantId && !x.Isdeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<StockLevel>> GetByPartAsync(Guid partId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<StockLevel>()
            .Include(x => x.Warehouse)
            .Where(x => x.PartId == partId && !x.Isdeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<StockLevel>> GetByWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<StockLevel>()
            .Include(x => x.Warehouse)
            .Where(x => x.WarehouseId == warehouseId && !x.Isdeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<StockLevel>> GetLowStockAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<StockLevel>()
            .Where(x => (x.QuantityOnHand - x.QuantityReserved) <= x.ReorderLevel && !x.Isdeleted)
            .ToListAsync(cancellationToken);
    }
}
