using AutoPartShop.Application.Suppliers;
using AutoPartShop.Application.Suppliers.Dtos;
using AutoPartShop.Domain.Entities;
using AutoPartsShop.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories
{
    public class SupplierReadRepository(AutoPartDbContext _db) : ISupplierReadRepository
    {

        public async Task<(IEnumerable<SupplierResponse> Suppliers, int TotalCount)> FindAllAsynce(SupplierQuery query, CancellationToken cancellationToken = default)
        {
            var term = query.Search.ToLower();
            var suppliers = _db.Suppliers
                .Where(x => !x.Isdeleted && (
                 (EF.Functions.Like(x.Name, $"%{term}%") ||
                 EF.Functions.Like(x.Country, $"%{term}%") ||
                 EF.Functions.Like(x.Phone, $"%{term}%") ||
                 EF.Functions.Like(x.Email, $"%{term}%") ||
                 EF.Functions.Like(x.City, $"%{term}%")
                )));


            if (query.Sorts != null && query.Sorts.Any())
            {
                var sorts =
                    query.Sorts.Select(x => (x.Field, x.Direction == "asc" ? true : false)).ToArray();
                suppliers = suppliers.OrderByMultiple(sorts);
            }
            else
            {
                suppliers = suppliers.OrderByDescending(x => x.CreatedDate);
            }

            var totalCount = await suppliers.CountAsync(cancellationToken);
            var items = await suppliers
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(supplier => new SupplierResponse
                {
                    Id = supplier.Id,
                    Name = supplier.Name,
                    Code = supplier.Code,
                    ContactPerson = supplier.ContactPerson,
                    Email = supplier.Email,
                    Phone = supplier.Phone,
                    Address = supplier.Address,
                    City = supplier.City,
                    State = supplier.State,
                    Country = supplier.Country,
                    PostalCode = supplier.PostalCode,
                    CurrentBalance = supplier.CurrentBalance,
                    IsActive = supplier.IsActive,
                    Rating = supplier.Rating,
                    CreatedBy = supplier.CreatedBy,
                    ModifiedBy = supplier.ModifiedBy
                })
                .ToListAsync(cancellationToken);

            // Balance is derived live from transactions, NOT from the stored Supplier.CurrentBalance
            // column (which defaults to 0 and is never updated). Formula mirrors
            // SupplierLedgerService.CalculateCurrentBalanceAsync: purchases - payments - refunds.
            // Positive balance => we owe the supplier.
            var ids = items.Select(i => i.Id).ToList();
            if (ids.Count > 0)
            {
                var purchases = await _db.PurchaseOrders
                    .Where(x => ids.Contains(x.SupplierId) &&
                                x.Status != "DRAFT" &&
                                x.Status != "SUBMITTED" &&
                                x.Status != "CANCELLED" &&
                                !x.Isdeleted)
                    .GroupBy(x => x.SupplierId)
                    .Select(g => new { SupplierId = g.Key, Total = g.Sum(x => x.TotalAmount) })
                    .ToDictionaryAsync(x => x.SupplierId, x => x.Total, cancellationToken);

                var payments = await _db.SupplierPayments
                    .Where(x => ids.Contains(x.SupplierId) &&
                                x.Status == "COMPLETED" &&
                                x.PaymentMethod != "REFUND" &&
                                (x.PaymentType == PaymentType.ADVANCE || x.SourceAdvancePaymentId == null) &&
                                !x.Isdeleted)
                    .GroupBy(x => x.SupplierId)
                    .Select(g => new { SupplierId = g.Key, Total = g.Sum(x => x.Amount) })
                    .ToDictionaryAsync(x => x.SupplierId, x => x.Total, cancellationToken);

                var refunds = await _db.PurchaseReturns
                    .Where(x => ids.Contains(x.SupplierId) &&
                                x.SettlementStatus == "SETTLED" &&
                                !x.Isdeleted)
                    .GroupBy(x => x.SupplierId)
                    .Select(g => new { SupplierId = g.Key, Total = g.Sum(x => x.SettledAmount) })
                    .ToDictionaryAsync(x => x.SupplierId, x => x.Total, cancellationToken);

                foreach (var item in items)
                {
                    item.CurrentBalance =
                        purchases.GetValueOrDefault(item.Id)
                        - payments.GetValueOrDefault(item.Id)
                        - refunds.GetValueOrDefault(item.Id);
                }
            }

            return (items, totalCount);
        }
    }
}
