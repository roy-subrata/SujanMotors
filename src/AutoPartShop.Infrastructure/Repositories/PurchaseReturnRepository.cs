using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class PurchaseReturnRepository : IPurchaseReturnRepository
{
    private readonly AutoPartDbContext _dbContext;

    public PurchaseReturnRepository(AutoPartDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<PurchaseReturn>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.PurchaseReturns
            .Include(x => x.LineItems)
                .ThenInclude(li => li.Part)
            .Include(x => x.Supplier)
            .Include(x => x.PurchaseOrder)
            .Where(x => !x.Isdeleted)
            .OrderByDescending(x => x.ReturnDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<PurchaseReturn?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PurchaseReturns
            .Include(x => x.LineItems)
                .ThenInclude(li => li.Part)
            .Include(x => x.Supplier)
            .Include(x => x.PurchaseOrder)
            .FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task AddAsync(PurchaseReturn entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        await _dbContext.PurchaseReturns.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PurchaseReturn entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _dbContext.PurchaseReturns.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.PurchaseReturns
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity != null)
        {
            _dbContext.PurchaseReturns.Remove(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PurchaseReturns
            .AnyAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task<PurchaseReturn?> GetByNumberAsync(string returnNumber, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PurchaseReturns
            .Include(x => x.LineItems)
                .ThenInclude(li => li.Part)
            .Include(x => x.Supplier)
            .Include(x => x.PurchaseOrder)
            .FirstOrDefaultAsync(x => x.ReturnNumber == returnNumber && !x.Isdeleted, cancellationToken);
    }

    public async Task<IEnumerable<PurchaseReturn>> GetByPurchaseOrderAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PurchaseReturns
            .Include(x => x.LineItems)
                .ThenInclude(li => li.Part)
            .Include(x => x.Supplier)
            .Include(x => x.PurchaseOrder)
            .Where(x => x.PurchaseOrderId == purchaseOrderId && !x.Isdeleted)
            .OrderByDescending(x => x.ReturnDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PurchaseReturn>> GetBySupplierAsync(Guid supplierId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PurchaseReturns
            .Include(x => x.LineItems)
                .ThenInclude(li => li.Part)
            .Include(x => x.Supplier)
            .Include(x => x.PurchaseOrder)
            .Where(x => x.SupplierId == supplierId && !x.Isdeleted)
            .OrderByDescending(x => x.ReturnDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PurchaseReturn>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PurchaseReturns
            .Include(x => x.LineItems)
                .ThenInclude(li => li.Part)
            .Include(x => x.Supplier)
            .Include(x => x.PurchaseOrder)
            .Where(x => x.Status == status && !x.Isdeleted)
            .OrderByDescending(x => x.ReturnDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PurchaseReturn>> GetPendingApprovalsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.PurchaseReturns
            .Include(x => x.LineItems)
                .ThenInclude(li => li.Part)
            .Include(x => x.Supplier)
            .Include(x => x.PurchaseOrder)
            .Where(x => x.Status == "PENDING" && !x.Isdeleted)
            .OrderBy(x => x.ReturnDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<PurchaseReturn> returns, int totalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.PurchaseReturns
            .Include(x => x.LineItems)
                .ThenInclude(li => li.Part)
            .Include(x => x.Supplier)
            .Include(x => x.PurchaseOrder)
            .Where(x => !x.Isdeleted)
            .OrderByDescending(x => x.ReturnDate);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IEnumerable<PurchaseReturn> returns, int totalCount)> SearchPagedAsync(string searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.PurchaseReturns
            .Include(x => x.LineItems)
                .ThenInclude(li => li.Part)
            .Include(x => x.Supplier)
            .Include(x => x.PurchaseOrder)
            .Where(x => !x.Isdeleted &&
                (x.ReturnNumber.Contains(searchTerm) ||
                 x.Reason.Contains(searchTerm) ||
                 x.Notes.Contains(searchTerm)))
            .OrderByDescending(x => x.ReturnDate);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<decimal> GetTotalSettledRefundsBySupplierAsync(Guid supplierId, CancellationToken cancellationToken = default)
        => await _dbContext.PurchaseReturns
            .Where(x => x.SupplierId == supplierId &&
                        x.SettlementStatus == "SETTLED" &&
                        !x.Isdeleted)
            .SumAsync(x => x.SettledAmount, cancellationToken);
}
