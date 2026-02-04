using AutoPartShop.Application.CustomerPayment;
using AutoPartShop.Application.CustomerPayment.Dtos;
using AutoPartShop.Application.DTOs.PaymentDtos;
using AutoPartsShop.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories
{
    public class CustomerPaymentReadRepository(AutoPartDbContext _dbContext) : ICustomerPaymentReadRepository
    {

        public async Task<(IEnumerable<Application.CustomerPayment.Dtos.CustomerPaymentResponse> payments, int totalCount)> FindAllAsync(CustomerPaymentQuery query, CancellationToken cancellationToken = default)
        {
            var paymentsQuery = _dbContext.CustomerPayments
                    .Include(x => x.Invoice)
                    .Include(x => x.Customer)
                    .Include(x => x.PaymentProvider)
                    .Include(x => x.SourceAdvancePayment)
                    .Where(x => !x.Isdeleted);

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.ToLower();
                paymentsQuery = paymentsQuery.Where(x =>
                    EF.Functions.Like(x.TransactionNumber.ToLower(), $"%{term}%") ||
                    (x.Invoice != null && EF.Functions.Like(x.Invoice.InvoiceNumber.ToLower(), $"%{term}%")) ||
                    EF.Functions.Like(x.PaymentMethod.ToLower(), $"%{term}%") ||
                    (x.Customer != null && EF.Functions.Like(x.Customer.FirstName.ToLower(), $"%{term}%")) ||
                    (x.Customer != null && EF.Functions.Like(x.Customer.LastName.ToLower(), $"%{term}%"))
                );
            }

            // Apply filters
            if (query.IsReconciled.HasValue)
            {
                paymentsQuery = paymentsQuery.Where(x => x.IsReconciled == query.IsReconciled.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.CustomerId) && Guid.TryParse(query.CustomerId, out var customerId))
            {
                paymentsQuery = paymentsQuery.Where(x => x.CustomerId == customerId);
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                paymentsQuery = paymentsQuery.Where(x => x.Status == query.Status);
            }

            if (query.FromDate.HasValue && query.ToDate.HasValue)
            {
                paymentsQuery = paymentsQuery.Where(x => x.PaymentDate >= query.FromDate.Value && x.PaymentDate <= query.ToDate.Value);
            }

            // Apply sorting
            paymentsQuery = paymentsQuery.ApplySorting(
                query.Sorts,
                x => x.PaymentDate,
                defaultAscending: false
            );

            // Get total count before pagination
            var totalCount = await paymentsQuery.CountAsync(cancellationToken);

            // Apply pagination
            var payments = await paymentsQuery
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            // Map to response DTOs
            var response = payments.Select(p => new Application.CustomerPayment.Dtos.CustomerPaymentResponse
            {
                Id = p.Id,
                CustomerId = p.CustomerId,
                CustomerName = p.Customer != null ? $"{p.Customer.FirstName} {p.Customer.LastName}".Trim() : string.Empty,
                InvoiceId = p.InvoiceId,
                InvoiceNumber = p.Invoice?.InvoiceNumber ?? string.Empty,
                PaymentProviderId = p.PaymentProviderId,
                ProviderName = p.PaymentProvider?.ProviderName ?? string.Empty,
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
                Notes = p.Notes,
                SettledDate = p.SettledDate,
                SettledBy = p.SettledBy,
                IsReconciled = p.IsReconciled,
                ReconciledDate = p.ReconciledDate,
                PaymentType = p.PaymentType.ToString(),
                RemainingAmount = p.RemainingAmount,
                SourceAdvancePaymentId = p.SourceAdvancePaymentId,
                SourceAdvanceTransactionNumber = p.SourceAdvancePayment?.TransactionNumber ?? string.Empty,
                CreatedAt = p.CreatedDate
            }).ToList();

            return (response, totalCount);
        }
    }


}
