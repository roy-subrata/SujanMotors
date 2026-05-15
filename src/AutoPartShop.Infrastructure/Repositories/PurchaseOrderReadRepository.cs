using AutoPartShop.Application.PurchaseOrders;
using AutoPartsShop.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class PurchaseOrderReadRepository(AutoPartDbContext _dbContext) : IPurchaseOrderReadRepository
{

    public async Task<(IEnumerable<PurchaseOrderResponse> response, int total)> FindAllAsync(PurcahseQueryDto query, CancellationToken cancellationToken = default)
    {
        var purchaseOrders = _dbContext.PurchaseOrders
               .Include(x => x.Supplier)
               .Where(x => !x.Isdeleted);

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.ToLower();
            purchaseOrders = purchaseOrders.Where(x =>
                EF.Functions.Like(x.PONumber.ToLower(), $"%{term}%") ||
                (x.Supplier != null && EF.Functions.Like(x.Supplier.Name.ToLower(), $"%{term}%"))
            );
        }

        if (!string.IsNullOrWhiteSpace(query.SupplierId) && Guid.TryParse(query.SupplierId, out var supplierId))
        {
            purchaseOrders = purchaseOrders.Where(x => x.SupplierId == supplierId);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var statuses = query.Status.Split(',').Select(s => s.Trim()).ToArray();
            purchaseOrders = purchaseOrders.Where(x => statuses.Contains(x.Status));
        }

        if (query.FromDate.HasValue && query.ToDate.HasValue)
        {
            purchaseOrders = purchaseOrders.Where(x => x.ExpectedDeliveryDate >= query.FromDate.Value && x.ExpectedDeliveryDate <= query.ToDate.Value);
        }
        if (query.Sorts != null && query.Sorts.Any())
        {
            var sorts =
                query.Sorts.Select(x => (x.Field, x.Direction == "asc" ? true : false)).ToArray();
            purchaseOrders = purchaseOrders.OrderByMultiple(sorts);
        }
        else
        {
            purchaseOrders = purchaseOrders.OrderBy(x => x.CreatedDate);
        }

        var totalCount = await purchaseOrders.CountAsync(cancellationToken);

        var items = await purchaseOrders
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(po => new PurchaseOrderResponse
            {
                Id = po.Id,
                PONumber = po.PONumber,
                SupplierId = po.SupplierId,
                SupplierName = po.Supplier != null ? po.Supplier.Name : string.Empty,
                SupplierCode = po.Supplier != null ? po.Supplier.Code : string.Empty,
                OrderDate = po.PODate,
                DeliveryDate = po.ExpectedDeliveryDate,
                Status = po.Status,
                PaymentStatus = po.PaymentStatus,
                SubTotal = po.SubTotal,
                TaxAmount = po.TaxAmount,
                TaxPercentage = po.TaxPercentage,
                Discount = po.DiscountAmount,
                DiscountPercentage = po.DiscountPercentage,
                GrandTotal = po.TotalAmount,
                AmountPaid = po.PaidAmount,
                OutstandingAmount = po.TotalAmount - po.PaidAmount,
                IsOverdue = DateTime.UtcNow > po.ExpectedDeliveryDate && po.Status != "DELIVERED" && po.Status != "CANCELLED",
                Notes = po.Notes,
                Lines = po.LineItems.Select(l => new PurchaseOrderLineResponse
                {
                    Id = l.Id,
                    PartId = l.PartId,
                    PartName = l.Part != null ? l.Part.Name : string.Empty,
                    PartBaseUnitId = l.Part != null ? l.Part.UnitId : null,
                    UnitId = l.UnitId,
                    UnitName = l.Unit != null ? l.Unit.Name : string.Empty,
                    UnitSymbol = l.Unit != null ? l.Unit.Symbol : string.Empty,
                    Quantity = l.Quantity,
                    QuantityInBaseUnit = l.QuantityInBaseUnit,
                    ReceivedQuantity = l.ReceivedQuantity,
                    ReceivedQuantityInBaseUnit = l.ReceivedQuantityInBaseUnit,
                    UnitPrice = l.UnitPrice,
                    LineTotal = l.TotalPrice,
                    PartDefaultSellingPrice = l.Part != null ? l.Part.SellingPrice : 0,
                    PartMinMarginPercent = 0m
                }).ToList(),
                CreatedAt = DateTime.UtcNow

            })
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<PurchaseOrderDto?> GetPurchaseOrderByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _dbContext.PurchaseOrders
             .Where(po => po.Id == id && !po.Isdeleted)
             .Select(po => new PurchaseOrderDto
             {
                 Id = po.Id,
                 PONumber = po.PONumber,
                 SupplierId = po.Supplier != null ? po.Supplier.Id : Guid.Empty,
                 SupplierName = po.Supplier != null ? po.Supplier.Name : string.Empty,
                 SupplierCode = po.Supplier != null ? po.Supplier.Code : string.Empty,
                 OrderDate = po.PODate,
                 DeliveryDate = po.ExpectedDeliveryDate,
                 Status = po.Status,
                 SubTotal = po.SubTotal,
                 TaxAmount = po.TaxAmount,
                 Discount = po.DiscountAmount,
                 DiscountPercentage = po.DiscountPercentage,
                 GrandTotal = po.TotalAmount,
                 Currency = po.Currency,
                 AmountPaid = po.PaidAmount,
                 OutstandingAmount = po.TotalAmount - po.PaidAmount,
                 IsOverdue = now > po.ExpectedDeliveryDate
                     && po.Status != "DELIVERED"
                     && po.Status != "CANCELLED",
                 CreatedAt = po.CreatedDate,
                 Notes = po.Notes,
                 TaxPercentage = po.TaxPercentage,
                 Lines = po.LineItems.Select(pl => new PurchaseOrderLineDto
                 {
                     Id = pl.Id,
                     LineTotal = pl.TotalPrice,
                     PartId = pl.PartId,
                     PartName = pl.Part != null ? pl.Part.Name : string.Empty,
                     PartBaseUnitId = pl.Part != null ? pl.Part.UnitId : null,
                     UnitPrice = pl.UnitPrice,
                     Quantity = pl.Quantity,
                     QuantityInBaseUnit = pl.QuantityInBaseUnit,
                     ReceivedQuantity = pl.ReceivedQuantity,
                     ReceivedQuantityInBaseUnit = pl.ReceivedQuantityInBaseUnit,
                     UnitId = pl.UnitId,
                     UnitName = pl.Unit != null ? pl.Unit.Name : string.Empty,
                     UnitSymbol = pl.Unit != null ? pl.Unit.Symbol : string.Empty,
                     PartDefaultSellingPrice = pl.Part != null ? pl.Part.SellingPrice : 0,
                     PartMinMarginPercent = 0m
                 }).ToList()
             })
             .FirstOrDefaultAsync(cancellationToken);
    }

}
