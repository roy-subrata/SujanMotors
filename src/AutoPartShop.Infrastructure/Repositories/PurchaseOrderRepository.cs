using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class PurchaseOrderRepository(AutoPartDbContext _db) : IPurchaseOrderRepository
{


    public async Task<IEnumerable<PurchaseOrder>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.PurchaseOrders
            .Include(p => p.LineItems)
            .Include(p => p.GoodsReceipts)
                .ThenInclude(gr => gr.LineItems)
            .Where(x => !x.Isdeleted)
            .ToListAsync(cancellationToken);
    }
    public async Task<PurchaseOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.PurchaseOrders
            .Include(p => p.LineItems)
            .Include(p => p.GoodsReceipts)
                .ThenInclude(gr => gr.LineItems)
            .FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }
    public async Task AddAsync(PurchaseOrder entity, CancellationToken cancellationToken = default)
    {
        _db.PurchaseOrders.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }
    public async Task UpdateAsync(PurchaseOrder entity, CancellationToken cancellationToken = default)
    {
        _db.PurchaseOrders.Update(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {

        var existing = await _db.PurchaseOrders.FirstOrDefaultAsync(x => x.Id == id);
        if (existing != null)
        {
            _db.PurchaseOrders.Remove(existing);
        }
        await _db.SaveChangesAsync(cancellationToken);
    }
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.PurchaseOrders.AnyAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task<PurchaseOrder?> GetByNumberAsync(string poNumber, CancellationToken cancellationToken = default) => await GetByIdAsync(Guid.Empty, cancellationToken);
    public async Task<IEnumerable<PurchaseOrder>> GetBySuppliersAsync(Guid supplierId, CancellationToken cancellationToken = default) => await GetAllAsync(cancellationToken);
    public async Task<IEnumerable<PurchaseOrder>> GetByStatusAsync(string status, CancellationToken cancellationToken = default) => await GetAllAsync(cancellationToken);
    public async Task<IEnumerable<PurchaseOrder>> GetOverdueAsync(CancellationToken cancellationToken = default) => await GetAllAsync(cancellationToken);
    public async Task<(IEnumerable<PurchaseOrder> orders, int totalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        var paged = all.Skip((pageNumber - 1) * pageSize).Take(pageSize);
        return (paged, all.Count());
    }
    public async Task<(IEnumerable<PurchaseOrder> orders, int totalCount)> SearchPagedAsync(string searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default) => await GetPagedAsync(pageNumber, pageSize, cancellationToken);
}
