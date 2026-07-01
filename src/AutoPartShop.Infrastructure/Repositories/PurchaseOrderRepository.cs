using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class PurchaseOrderRepository(AutoPartDbContext _db) : IPurchaseOrderRepository
{


    public async Task<IEnumerable<PurchaseOrder>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.PurchaseOrders
            .Include(p => p.Supplier)
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
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }
    public async Task AddAsync(PurchaseOrder entity, CancellationToken cancellationToken = default)
    {
        _db.PurchaseOrders.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }
    public async Task UpdateAsync(PurchaseOrder entity, CancellationToken cancellationToken = default)
    {
        // If the same PO was already loaded as a navigation property on another entity (e.g.
        // GoodsReceipt.PurchaseOrder) it sits in the change tracker as a different object instance.
        // Attaching a second instance with the same key throws "already being tracked". Detach the
        // stale instance first so the updated one can be attached cleanly.
        var stale = _db.Set<PurchaseOrder>().Local.FirstOrDefault(e => e.Id == entity.Id);
        if (stale != null && !ReferenceEquals(stale, entity))
            _db.Entry(stale).State = EntityState.Detached;

        // Get existing line item IDs from database
        var existingLineItemIds = await _db.Set<PurchaseOrderLine>()
            .Where(l => l.PurchaseOrderId == entity.Id)
            .Select(l => l.Id)
            .ToListAsync(cancellationToken);

        var existingLineItemIdSet = existingLineItemIds.ToHashSet();

        // Find line items to delete (existing IDs not in the updated entity)
        var currentLineItemIds = entity.LineItems.Select(l => l.Id).ToHashSet();
        var lineItemIdsToDelete = existingLineItemIds.Where(id => !currentLineItemIds.Contains(id)).ToList();

        // Delete removed line items
        if (lineItemIdsToDelete.Count > 0)
        {
            await _db.Set<PurchaseOrderLine>()
                .Where(l => lineItemIdsToDelete.Contains(l.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        // Attach the purchase order and mark as modified
        _db.Entry(entity).State = EntityState.Modified;

        // Handle line items individually - mark new ones as Added, existing ones as Modified
        foreach (var lineItem in entity.LineItems)
        {
            if (existingLineItemIdSet.Contains(lineItem.Id))
            {
                // Existing line item - mark as modified
                _db.Entry(lineItem).State = EntityState.Modified;
            }
            else
            {
                // New line item - mark as added
                _db.Entry(lineItem).State = EntityState.Added;
            }
        }

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

    public async Task<PurchaseOrder?> GetByNumberAsync(string poNumber, CancellationToken cancellationToken = default)
    {
        return await _db.PurchaseOrders
            .Include(p => p.LineItems)
            .Include(p => p.GoodsReceipts)
                .ThenInclude(gr => gr.LineItems)
            .FirstOrDefaultAsync(x => x.PONumber == poNumber && !x.Isdeleted, cancellationToken);
    }

    public async Task<IEnumerable<PurchaseOrder>> GetBySuppliersAsync(Guid supplierId, CancellationToken cancellationToken = default)
    {
        return await _db.PurchaseOrders
            .Include(p => p.LineItems)
            .Include(p => p.GoodsReceipts)
                .ThenInclude(gr => gr.LineItems)
            .Where(x => x.SupplierId == supplierId && !x.Isdeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PurchaseOrder>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        return await _db.PurchaseOrders
            .Include(p => p.LineItems)
            .Include(p => p.GoodsReceipts)
                .ThenInclude(gr => gr.LineItems)
            .Where(x => x.Status == status && !x.Isdeleted)
            .ToListAsync(cancellationToken);
    }
    public async Task<IEnumerable<PurchaseOrder>> GetOverdueAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        return await _db.PurchaseOrders
            .Include(p => p.LineItems)
            .Include(p => p.GoodsReceipts)
                .ThenInclude(gr => gr.LineItems)
            .Where(x => x.ExpectedDeliveryDate < today && x.Status != "COMPLETED" && x.Status != "CANCELLED" && !x.Isdeleted)
            .ToListAsync(cancellationToken);
    }
    public async Task<(IEnumerable<PurchaseOrder> orders, int totalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        var paged = all.Skip((pageNumber - 1) * pageSize).Take(pageSize);
        return (paged, all.Count());
    }
    public async Task<(IEnumerable<PurchaseOrder> orders, int totalCount)> SearchPagedAsync(string searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default) => await GetPagedAsync(pageNumber, pageSize, cancellationToken);

    public async Task<decimal> GetTotalPurchaseAmountBySupplierAsync(Guid supplierId, CancellationToken cancellationToken = default)
        => await _db.PurchaseOrders
            .Where(x => x.SupplierId == supplierId &&
                        x.Status != "DRAFT" &&
                        x.Status != "SUBMITTED" &&
                        x.Status != "CANCELLED" &&
                        !x.Isdeleted)
            .SumAsync(x => x.TotalAmount, cancellationToken);
}

