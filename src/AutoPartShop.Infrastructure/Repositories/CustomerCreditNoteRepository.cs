using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class CustomerCreditNoteRepository(AutoPartDbContext dbContext) : ICustomerCreditNoteRepository
{
    public async Task<CustomerCreditNote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.CustomerCreditNotes
            .Include(cn => cn.Customer)
            .Include(cn => cn.SalesReturn)
            .Include(cn => cn.Invoice)
            .Include(cn => cn.SalesOrder)
            .FirstOrDefaultAsync(cn => cn.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<CustomerCreditNote>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await dbContext.CustomerCreditNotes
            .Include(cn => cn.Customer)
            .Include(cn => cn.SalesReturn)
            .Where(cn => cn.CustomerId == customerId)
            .OrderByDescending(cn => cn.IssueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CustomerCreditNote>> GetAvailableCreditsAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await dbContext.CustomerCreditNotes
            .Include(cn => cn.Customer)
            .Where(cn => cn.CustomerId == customerId
                      && cn.Status != "CANCELLED"
                      && cn.Status != "EXPIRED"
                      && cn.Status != "FULLY_USED"
                      && (!cn.ExpiryDate.HasValue || cn.ExpiryDate.Value >= DateTime.UtcNow))
            .OrderBy(cn => cn.ExpiryDate)
            .ThenBy(cn => cn.IssueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<CustomerCreditNote> CreditNotes, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.CustomerCreditNotes
            .Include(cn => cn.Customer)
            .Include(cn => cn.SalesReturn)
            .OrderByDescending(cn => cn.IssueDate);

        var totalCount = await query.CountAsync(cancellationToken);
        var creditNotes = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (creditNotes, totalCount);
    }

    public async Task<(IEnumerable<CustomerCreditNote> CreditNotes, int TotalCount)> SearchPagedAsync(
        CustomerCreditNoteQuery query, CancellationToken cancellationToken = default)
    {
        var dbQuery = dbContext.CustomerCreditNotes
            .Include(cn => cn.Customer)
            .Include(cn => cn.SalesReturn)
            .AsQueryable();

        if (query.CustomerId.HasValue)
            dbQuery = dbQuery.Where(cn => cn.CustomerId == query.CustomerId.Value);

        if (!string.IsNullOrWhiteSpace(query.Status))
            dbQuery = dbQuery.Where(cn => cn.Status == query.Status);

        var totalCount = await dbQuery.CountAsync(cancellationToken);
        var creditNotes = await dbQuery
            .OrderByDescending(cn => cn.IssueDate)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (creditNotes, totalCount);
    }

    public async Task AddAsync(CustomerCreditNote entity, CancellationToken cancellationToken = default)
    {
        await dbContext.CustomerCreditNotes.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(CustomerCreditNote entity, CancellationToken cancellationToken = default)
    {
        dbContext.CustomerCreditNotes.Update(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<decimal> GetTotalAvailableCreditAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await dbContext.CustomerCreditNotes
            .Where(cn => cn.CustomerId == customerId
                      && cn.Status != "CANCELLED"
                      && cn.Status != "EXPIRED"
                      && cn.Status != "FULLY_USED"
                      && (!cn.ExpiryDate.HasValue || cn.ExpiryDate.Value >= DateTime.UtcNow))
            .SumAsync(cn => cn.TotalAmount - cn.UsedAmount, cancellationToken);
    }
}
