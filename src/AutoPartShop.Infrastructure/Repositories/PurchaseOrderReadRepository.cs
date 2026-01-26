using AutoPartShop.Application.DTOs.CustomerDtos;
using AutoPartShop.Application.DTOs.PurchaseOrderDtos;
using AutoPartShop.Application.PurchaseOrders;
using AutoPartShop.Domain.Common;
using AutoPartsShop.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class PurchaseOrderReadRepository(AutoPartDbContext _dbContext) : IPurchaseOrderReadRepository
{
    public async Task<PaginatedResponse<PurchaseOrderResponse>> GetPurchaseOrderAsync(PurcahseQueryDto query, CancellationToken cancellationToken = default)
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
            purchaseOrders = purchaseOrders.Where(x => x.Status == query.Status);
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
        var porders = await purchaseOrders
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
                PaymentStatus=po.PaymentStatus,
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
                    UnitId = l.UnitId,
                    UnitName = l.Unit != null ? l.Unit.Name : string.Empty,
                    UnitSymbol = l.Unit != null ? l.Unit.Symbol : string.Empty,
                    Quantity = l.Quantity,
                    QuantityInBaseUnit = l.QuantityInBaseUnit,
                    ReceivedQuantity = l.ReceivedQuantity,
                    ReceivedQuantityInBaseUnit = l.ReceivedQuantityInBaseUnit,
                    UnitPrice = l.UnitPrice,
                    LineTotal = l.TotalPrice
                }).ToList(),
                CreatedAt = DateTime.UtcNow

            })
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<PurchaseOrderResponse>
        {
            Data = porders,
            Pagination = new PaginationMeta
            {
                PageNumber = query.PageNumber,
                PageSize = query.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
            }
        };
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
                     UnitPrice = pl.UnitPrice,
                     Quantity = pl.Quantity,
                     QuantityInBaseUnit = pl.QuantityInBaseUnit,
                     ReceivedQuantity = pl.ReceivedQuantity,
                     ReceivedQuantityInBaseUnit = pl.ReceivedQuantityInBaseUnit,
                     UnitId = pl.UnitId,
                     UnitName = pl.Unit != null ? pl.Unit.Name : string.Empty,
                     UnitSymbol = pl.Unit != null ? pl.Unit.Symbol : string.Empty
                 }).ToList()
             })
             .FirstOrDefaultAsync(cancellationToken);
    }



}