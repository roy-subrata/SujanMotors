using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class StockLotRepository : IStockLotRepository
{
    private readonly AutoPartDbContext _dbContext;

    public StockLotRepository(AutoPartDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<StockLot>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockLots
            .Where(x => !x.Isdeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<StockLot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockLots
            .FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task AddAsync(StockLot entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        await _dbContext.StockLots.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(StockLot entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _dbContext.StockLots.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.StockLots
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity != null)
        {
            _dbContext.StockLots.Remove(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockLots
            .AnyAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task<IEnumerable<StockLot>> GetByPartAsync(Guid partId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockLots
            .Where(x => x.PartId == partId && !x.Isdeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<StockLot>> GetByWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockLots
            .Where(x => x.WarehouseId == warehouseId && !x.Isdeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<StockLot>> GetAvailableLotsAsync(Guid partId, Guid warehouseId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockLots
            .Where(x => x.PartId == partId && x.WarehouseId == warehouseId && x.QuantityAvailable > 0 && !x.Isdeleted)
            .OrderBy(x => x.ReceivingDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<StockLot>> GetAvailableLotsAsync(Guid partId, Guid? variantId, Guid warehouseId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockLots
            .Where(x => x.PartId == partId && x.VariantId == variantId && x.WarehouseId == warehouseId && x.QuantityAvailable > 0 && !x.Isdeleted)
            .OrderBy(x => x.ReceivingDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<StockLot?> GetByLotNumberAsync(string lotNumber, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockLots
            .FirstOrDefaultAsync(x => x.LotNumber == lotNumber && !x.Isdeleted, cancellationToken);
    }

    public async Task<IEnumerable<StockLot>> GetBySupplierAsync(Guid supplierId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockLots
            .Where(x => x.SupplierId == supplierId && !x.Isdeleted)
            .OrderByDescending(x => x.ReceivingDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<StockLot>> GetByPartAndWarehouseAsync(Guid partId, Guid warehouseId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockLots
            .Where(x => x.PartId == partId && x.WarehouseId == warehouseId && !x.Isdeleted)
            .OrderBy(x => x.ReceivingDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<StockLot>> GetExpiredLotsAsync(CancellationToken cancellationToken = default)
    {
        var currentDate = DateTime.UtcNow.Date;
        return await _dbContext.StockLots
            .Where(x => x.ExpiryDate.HasValue && x.ExpiryDate.Value < currentDate && !x.Isdeleted)
            .OrderBy(x => x.ExpiryDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<StockLot>> GetLowStockLotsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockLots
            .Where(x => x.QuantityAvailable > 0 && x.QuantityAvailable < 10 && !x.Isdeleted)
            .OrderBy(x => x.QuantityAvailable)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<StockLot>> GetByGoodsReceiptLineAsync(Guid goodsReceiptLineId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockLots
            .Where(x => x.GoodsReceiptLineId == goodsReceiptLineId && !x.Isdeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<StockLot> items, int totalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.StockLots
            .Where(x => !x.Isdeleted)
            .OrderByDescending(x => x.ReceivingDate);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
