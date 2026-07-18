using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class QuotationRepository(AutoPartDbContext dbContext) : IQuotationRepository
{
    public async Task<Quotation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Quotations
            .Include(q => q.Customer)
            .Include(q => q.LineItems).ThenInclude(l => l.Part)
            .Include(q => q.LineItems).ThenInclude(l => l.ProductVariant)
            .Include(q => q.LineItems).ThenInclude(l => l.Unit)
            .FirstOrDefaultAsync(q => q.Id == id && !q.Isdeleted, cancellationToken);
    }

    public async Task<Quotation?> GetByNumberAsync(string quotationNumber, CancellationToken cancellationToken = default)
    {
        return await dbContext.Quotations
            .Include(q => q.Customer)
            .Include(q => q.LineItems).ThenInclude(l => l.Part)
            .FirstOrDefaultAsync(q => q.QuotationNumber == quotationNumber.Trim().ToUpper() && !q.Isdeleted, cancellationToken);
    }

    public async Task<IEnumerable<Quotation>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Quotations
            .Include(q => q.Customer)
            .Where(q => q.CustomerId == customerId && !q.Isdeleted)
            .OrderByDescending(q => q.QuoteDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<Quotation> Quotations, int TotalCount)> SearchPagedAsync(
        QuotationQuery query, CancellationToken cancellationToken = default)
    {
        var dbQuery = dbContext.Quotations
            .Include(q => q.Customer)
            .Where(q => !q.Isdeleted)
            .AsQueryable();

        if (query.CustomerId.HasValue)
            dbQuery = dbQuery.Where(q => q.CustomerId == query.CustomerId.Value);

        if (!string.IsNullOrWhiteSpace(query.Status))
            dbQuery = dbQuery.Where(q => q.Status == query.Status);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            dbQuery = dbQuery.Where(q =>
                q.QuotationNumber.Contains(term) || q.CustomerName.Contains(term));
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);
        var quotations = await dbQuery
            .OrderByDescending(q => q.QuoteDate)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (quotations, totalCount);
    }

    public async Task AddAsync(Quotation entity, CancellationToken cancellationToken = default)
    {
        await dbContext.Quotations.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Quotation entity, CancellationToken cancellationToken = default)
    {
        dbContext.Quotations.Update(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
