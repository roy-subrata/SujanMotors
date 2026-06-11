using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class GoodsReceiptRepository(AutoPartDbContext _db) : IGoodsReceiptRepository
{
    private IQueryable<GoodsReceipt> QueryWithDetails()
    {
        return _db.GoodsReceipts
            .Where(x => !x.Isdeleted)
            .Include(x => x.PurchaseOrder)
            .Include(x => x.Warehouse)
            .Include(x => x.LineItems)
                .ThenInclude(l => l.Part)
            .Include(x => x.LineItems)
                .ThenInclude(l => l.Variant);
    }

    public async Task<IEnumerable<GoodsReceipt>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await QueryWithDetails()
            .OrderByDescending(x => x.ReceiptDate)
            .ToListAsync(cancellationToken);
    }
    public async Task<GoodsReceipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await QueryWithDetails()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
    public async Task AddAsync(GoodsReceipt entity, CancellationToken cancellationToken = default)
    {
        _db.GoodsReceipts.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }
    public async Task UpdateAsync(GoodsReceipt entity, CancellationToken cancellationToken = default)
    {
        if (_db.Entry(entity).State == EntityState.Detached)
        {
            _db.GoodsReceipts.Update(entity);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var e = await _db.GoodsReceipts.FirstOrDefaultAsync(x => x.Id == id);
        if (e != null) _db.GoodsReceipts.Remove(e);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.GoodsReceipts.AnyAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task<GoodsReceipt?> GetByNumberAsync(string grnNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(grnNumber))
            return null;

        var normalizedGrnNumber = grnNumber.Trim().ToUpperInvariant();

        return await QueryWithDetails()
            .FirstOrDefaultAsync(x => x.GRNNumber.ToUpper() == normalizedGrnNumber, cancellationToken);
    }
    public async Task<IEnumerable<GoodsReceipt>> GetByPurchaseOrderAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default)
    {
        return await QueryWithDetails()
            .Where(x => x.PurchaseOrderId == purchaseOrderId)
            .OrderByDescending(x => x.ReceiptDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<GoodsReceipt>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(status))
            return Enumerable.Empty<GoodsReceipt>();

        var normalizedStatus = status.Trim().ToUpperInvariant();

        return await QueryWithDetails()
            .Where(x => x.Status.ToUpper() == normalizedStatus)
            .OrderByDescending(x => x.ReceiptDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<GoodsReceipt>> GetPendingVerificationAsync(CancellationToken cancellationToken = default)
    {
        return await GetByStatusAsync("PENDING", cancellationToken);
    }

    public async Task<(IEnumerable<GoodsReceipt> receipts, int totalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;

        var query = QueryWithDetails().OrderByDescending(x => x.ReceiptDate);
        var totalCount = await query.CountAsync(cancellationToken);
        var paged = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (paged, totalCount);
    }
}
