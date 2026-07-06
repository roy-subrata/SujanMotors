using AutoPartShop.Application.Common;
using AutoPartShop.Application.Supplier;
using AutoPartShop.Application.SupplierPayment.Dtos;
using AutoPartShop.Domain.Entities;
using AutoPartsShop.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories
{
    public class SupplierPaymentReadRespository(AutoPartDbContext _db) : ISupplierPaymentReadRespository
    {
        public async Task<(IEnumerable<SupplierPaymentResponse> paymentResponse, int total)> FindAllAsynce(SupplierPaymentQuery query, CancellationToken cancellationToken = default)
        {
            var paymentsQuery = BuildSearchQuery(query);

            // Get total count before pagination
            var totalCount = await paymentsQuery.CountAsync(cancellationToken);

            // Apply sorting
            paymentsQuery = paymentsQuery.ApplySorting(
                query.Sorts,
                x => x.PaymentDate,
                defaultAscending: false
            );

            // Apply pagination
            var payments = await paymentsQuery
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            // Map to response DTOs
            var responses = payments.Select(p => new SupplierPaymentResponse
            {
                Id = p.Id,
                SupplierId = p.SupplierId,
                SupplierName = p.Supplier?.Name ?? string.Empty,
                PurchaseOrderId = p.PurchaseOrderId,
                GoodsReceiptId = p.GoodsReceiptId,
                PaymentProviderId = p.PaymentProviderId,
                ProviderName = p.PaymentProvider?.ProviderName ?? string.Empty,
                SupplierPaymentAccountId = p.SupplierPaymentAccountId,
                SupplierPaymentAccountName = p.SupplierPaymentAccount?.GetDisplayText() ?? string.Empty,
                TransactionNumber = p.TransactionNumber,
                Amount = p.Amount,
                PaymentFee = p.PaymentFee,
                NetAmount = p.NetAmount,
                Currency = p.Currency,
                PaymentDate = p.PaymentDate,
                PaymentMethod = p.PaymentMethod,
                Status = p.Status,
                ReferenceNumber = p.ReferenceNumber,
                AuthorizationCode = p.AuthorizationCode,
                InvoiceNumber = p.InvoiceNumber,
                Notes = p.Notes,
                ProcessedDate = p.ProcessedDate,
                ProcessedBy = p.ProcessedBy,
                ConfirmedDate = p.ConfirmedDate,
                ConfirmedBy = p.ConfirmedBy,
                IsReconciled = p.IsReconciled,
                ReconciledDate = p.ReconciledDate,
                CreatedAt = p.CreatedDate,
                PaymentType = p.PaymentType,
                Description = p.Description,
                RemainingAmount = p.RemainingAmount,
                SourceAdvancePaymentId = p.SourceAdvancePaymentId
            }).ToList();

            return (responses, totalCount);
        }


        private IQueryable<SupplierPayment> BuildSearchQuery(SupplierPaymentQuery query)
        {
            var paymentsQuery = _db.SupplierPayments
                .Include(x => x.Supplier)
                .Include(x => x.GoodsReceipt)
                .Include(x => x.PaymentProvider)
                .Include(x => x.SupplierPaymentAccount)
                .Where(x => !x.Isdeleted);

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.ToLower();
                paymentsQuery = paymentsQuery.Where(x =>
                    EF.Functions.Like(x.TransactionNumber.ToLower(), $"%{term}%") ||
                    (x.GoodsReceipt != null && EF.Functions.Like(x.GoodsReceipt.GRNNumber.ToLower(), $"%{term}%")) ||
                    EF.Functions.Like(x.PaymentMethod.ToLower(), $"%{term}%") ||
                    (x.Supplier != null && EF.Functions.Like(x.Supplier.Name.ToLower(), $"%{term}%"))
                );
            }

            // Apply filters
            if (query.IsReconciled.HasValue)
            {
                paymentsQuery = paymentsQuery.Where(x => x.IsReconciled == query.IsReconciled.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.SupplierId) && Guid.TryParse(query.SupplierId, out var supplierId))
            {
                paymentsQuery = paymentsQuery.Where(x => x.SupplierId == supplierId);
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                paymentsQuery = paymentsQuery.Where(x => x.Status == query.Status);
            }

            if (query.FromDate.HasValue && query.ToDate.HasValue)
            {
                paymentsQuery = paymentsQuery.Where(x => x.PaymentDate >= query.FromDate.Value && x.PaymentDate <= query.ToDate.Value);
            }

            return paymentsQuery;
        }

    }
}
