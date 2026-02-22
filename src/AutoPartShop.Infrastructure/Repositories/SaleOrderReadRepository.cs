using AutoPartShop.Application.SaleOrders;
using AutoPartShop.Application.SaleOrders.Dtos;
using AutoPartsShop.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories
{
    public class SaleOrderReadRepository(AutoPartDbContext _dbContext) : ISaleOrderReadRepository
    {
        public async Task<(IReadOnlyCollection<SaleOrderResponse> response, int totalCount)> FindAllQuery(SaleOrderQuery query, CancellationToken cancellationToken)
        {
            var salesOrders = _dbContext.SalesOrders
                .Include(x => x.LineItems)
                    .ThenInclude(li => li.Part)
                .Where(x => !x.Isdeleted);

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.ToLower();
                salesOrders = salesOrders.Where(x =>
                    EF.Functions.Like(x.SONumber.ToLower(), $"%{term}%") ||
                    (x.Invoice != null && EF.Functions.Like(x.CustomerEmail.ToLower(), $"%{term}%")) ||
                    EF.Functions.Like(x.CustomerName.ToLower(), $"%{term}%") ||
                    (x.Customer != null && EF.Functions.Like(x.CustomerPhone.ToLower(), $"%{term}%"))
                );
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                salesOrders = salesOrders.Where(x => x.Status == query.Status);
            }


            if (query.FromDate.HasValue && query.ToDate.HasValue)
            {
                salesOrders = salesOrders.Where(x => x.SODate >= query.FromDate.Value && x.SODate <= query.ToDate.Value);
            }

            salesOrders = salesOrders.ApplySorting(
               query.Sorts,
               x => x.SODate,
               defaultAscending: false
           );

            var totalCount = await salesOrders.CountAsync(cancellationToken);
            var items = await salesOrders
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(order => new SaleOrderResponse()
                {
                    Id = order.Id,
                    SONumber = order.SONumber,
                    CustomerId = order.CustomerId,
                    CustomerName = order.CustomerName,
                    CustomerEmail = order.CustomerEmail,
                    CustomerPhone = order.CustomerPhone,
                    CustomerCity = order.DeliveryAddress,
                    WarehouseId = order.WarehouseId,
                    TechnicianId = order.TechnicianId,
                    TechnicianName = order.TechnicianName,
                    OrderDate = order.SODate,
                    DeliveryDate = order.DeliveryDate ?? DateTime.MinValue,
                    Status = order.Status,
                    SubTotal = order.SubTotal,
                    TaxAmount = order.TaxAmount,
                    Discount = order.DiscountPercentage,
                    GrandTotal = order.GrandTotal,
                    Currency = order.Currency,
                    AmountPaid = order.PaidAmount,
                    OutstandingAmount = order.GrandTotal - order.PaidAmount,
                    IsOverdue = order.DeliveryDate.HasValue && DateTime.UtcNow > order.DeliveryDate.Value && order.Status != "DELIVERED" && order.Status != "CANCELLED",
                    Notes = order.Notes,
                    Lines = order.LineItems.Select(l => new SalesOrderLineResponse
                    {
                        Id = l.Id,
                        PartId = l.PartId,
                        PartName = l.Part != null ? l.Part.Name : string.Empty,
                        PartSku = l.Part != null ? l.Part.SKU : string.Empty,
                        UnitId = l.UnitId,
                        UnitName = l.Unit != null ? l.Unit.Name : string.Empty,
                        UnitSymbol = l.Unit != null ? l.Unit.Symbol : string.Empty,
                        Quantity = l.Quantity,
                        QuantityInBaseUnit = l.QuantityInBaseUnit,
                        ShippedQuantity = l.ShippedQuantity,
                        ShippedQuantityInBaseUnit = l.ShippedQuantityInBaseUnit,
                        UnitPrice = l.UnitPrice,
                        Discount = l.UnitPrice == 0 ? 0 : Math.Round((l.Discount / l.UnitPrice) * 100, 2),
                        LineTotal = l.TotalPrice
                    }).ToList(),
                    CreatedAt = DateTime.UtcNow
                })
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }
    }
}
