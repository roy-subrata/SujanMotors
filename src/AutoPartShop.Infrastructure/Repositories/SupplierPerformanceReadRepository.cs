using AutoPartShop.Application.Suppliers;
using AutoPartShop.Application.Suppliers.Dtos;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class SupplierPerformanceReadRepository(AutoPartDbContext _db) : ISupplierPerformanceReadRepository
{
    public async Task<IEnumerable<SupplierPerformanceResponse>> GetPerformanceAsync(
        string? search = null, CancellationToken cancellationToken = default)
    {
        // Flat per-GRN aggregation over ACCEPTED receipts, joined to PO -> Supplier.
        // Grouped in memory afterwards (reports are low-volume).
        var rows = await (
            from g in _db.GoodsReceipts.Where(g => g.Status == "ACCEPTED" && !g.Isdeleted)
            join po in _db.PurchaseOrders on g.PurchaseOrderId equals po.Id
            join s in _db.Suppliers on po.SupplierId equals s.Id
            select new
            {
                s.Id,
                s.Name,
                s.Code,
                Received = g.LineItems.Sum(l => (int?)l.ReceivedQuantity) ?? 0,
                // RejectedQuantity is a computed (unmapped) property — sum the stored buckets so EF can translate to SQL.
                Rejected = g.LineItems.Sum(l => (int?)(l.DamagedQuantity + l.WrongQuantity)) ?? 0
            })
            .ToListAsync(cancellationToken);

        // Purchase returns raised per supplier (any status).
        var returnCounts = await _db.PurchaseReturns
            .Where(r => !r.Isdeleted)
            .GroupBy(r => r.SupplierId)
            .Select(grp => new { SupplierId = grp.Key, Count = grp.Count() })
            .ToListAsync(cancellationToken);

        var returnsBySupplier = returnCounts.ToDictionary(x => x.SupplierId, x => x.Count);

        var result = rows
            .GroupBy(r => new { r.Id, r.Name, r.Code })
            .Select(grp =>
            {
                var received = grp.Sum(x => x.Received);
                var rejected = grp.Sum(x => x.Rejected);
                return new SupplierPerformanceResponse
                {
                    SupplierId = grp.Key.Id,
                    SupplierName = grp.Key.Name,
                    SupplierCode = grp.Key.Code,
                    GrnCount = grp.Count(),
                    TotalReceivedQty = received,
                    TotalRejectedQty = rejected,
                    TotalAcceptedQty = received - rejected,
                    ReturnCount = returnsBySupplier.TryGetValue(grp.Key.Id, out var c) ? c : 0,
                    DamagedRatePct = received > 0 ? Math.Round((decimal)rejected / received * 100m, 2) : 0m
                };
            });

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            result = result.Where(r =>
                r.SupplierName.ToLower().Contains(term) ||
                r.SupplierCode.ToLower().Contains(term));
        }

        return result
            .OrderByDescending(r => r.DamagedRatePct)
            .ThenBy(r => r.SupplierName)
            .ToList();
    }
}
