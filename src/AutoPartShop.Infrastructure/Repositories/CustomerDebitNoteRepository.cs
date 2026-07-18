using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class CustomerDebitNoteRepository(AutoPartDbContext dbContext) : ICustomerDebitNoteRepository
{
    public async Task<CustomerDebitNote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.CustomerDebitNotes
            .Include(dn => dn.Customer)
            .Include(dn => dn.Invoice)
            .FirstOrDefaultAsync(dn => dn.Id == id && !dn.Isdeleted, cancellationToken);
    }

    public async Task<IEnumerable<CustomerDebitNote>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await dbContext.CustomerDebitNotes
            .Include(dn => dn.Customer)
            .Where(dn => dn.CustomerId == customerId && !dn.Isdeleted)
            .OrderByDescending(dn => dn.IssueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<CustomerDebitNote> DebitNotes, int TotalCount)> SearchPagedAsync(
        CustomerDebitNoteQuery query, CancellationToken cancellationToken = default)
    {
        var dbQuery = dbContext.CustomerDebitNotes
            .Include(dn => dn.Customer)
            .Where(dn => !dn.Isdeleted)
            .AsQueryable();

        if (query.CustomerId.HasValue)
            dbQuery = dbQuery.Where(dn => dn.CustomerId == query.CustomerId.Value);

        if (!string.IsNullOrWhiteSpace(query.Status))
            dbQuery = dbQuery.Where(dn => dn.Status == query.Status);

        var totalCount = await dbQuery.CountAsync(cancellationToken);
        var debitNotes = await dbQuery
            .OrderByDescending(dn => dn.IssueDate)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (debitNotes, totalCount);
    }

    public async Task AddAsync(CustomerDebitNote entity, CancellationToken cancellationToken = default)
    {
        await dbContext.CustomerDebitNotes.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(CustomerDebitNote entity, CancellationToken cancellationToken = default)
    {
        dbContext.CustomerDebitNotes.Update(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
