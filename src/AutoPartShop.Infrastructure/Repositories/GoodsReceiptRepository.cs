using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class GoodsReceiptRepository(AutoPartDbContext _db) : IGoodsReceiptRepository
{

    public async Task<IEnumerable<GoodsReceipt>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.GoodsReceipts.Where(x => !x.Isdeleted).ToListAsync(cancellationToken);
    }
    public async Task<GoodsReceipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.GoodsReceipts.Include(p => p.LineItems).FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }
    public async Task AddAsync(GoodsReceipt entity, CancellationToken cancellationToken = default)
    {
        _db.GoodsReceipts.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }
    public async Task UpdateAsync(GoodsReceipt entity, CancellationToken cancellationToken = default)
    {
        var esixitng = await _db.GoodsReceipts.FirstOrDefaultAsync(x => x.Id == entity.Id);
        if (esixitng != null)
        {
            //Todo
           // _db.GoodsReceipts.Remove(esixitng);
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
        return await GetByIdAsync(Guid.Empty, cancellationToken);
    }
    public async Task<IEnumerable<GoodsReceipt>> GetByPurchaseOrderAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default) => await GetAllAsync(cancellationToken);
    public async Task<IEnumerable<GoodsReceipt>> GetByStatusAsync(string status, CancellationToken cancellationToken = default) => await GetAllAsync(cancellationToken);
    public async Task<IEnumerable<GoodsReceipt>> GetPendingVerificationAsync(CancellationToken cancellationToken = default) => await GetAllAsync(cancellationToken);
    public async Task<(IEnumerable<GoodsReceipt> receipts, int totalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        var paged = all.Skip((pageNumber - 1) * pageSize).Take(pageSize);
        return (paged, all.Count());
    }
}
