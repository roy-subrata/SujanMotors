using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class CreditNoteRepository(AutoPartDbContext dbContext) : ICreditNoteRepository
{
    public async Task<CreditNote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.CreditNotes
            .Include(cn => cn.Supplier)
            .Include(cn => cn.PurchaseReturn)
            .Include(cn => cn.PurchaseOrder)
            .FirstOrDefaultAsync(cn => cn.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<CreditNote>> GetBySupplierIdAsync(Guid supplierId, CancellationToken cancellationToken = default)
    {
        return await dbContext.CreditNotes
            .Include(cn => cn.Supplier)
            .Include(cn => cn.PurchaseReturn)
            .Where(cn => cn.SupplierId == supplierId)
            .OrderByDescending(cn => cn.IssueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CreditNote>> GetAvailableCreditsAsync(Guid supplierId, CancellationToken cancellationToken = default)
    {
        return await dbContext.CreditNotes
            .Include(cn => cn.Supplier)
            .Where(cn => cn.SupplierId == supplierId
                      && cn.Status != "CANCELLED"
                      && cn.Status != "EXPIRED"
                      && cn.Status != "FULLY_USED"
                      && (!cn.ExpiryDate.HasValue || cn.ExpiryDate.Value >= DateTime.UtcNow))
            .OrderBy(cn => cn.ExpiryDate)
            .ThenBy(cn => cn.IssueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<CreditNote> CreditNotes, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.CreditNotes
            .Include(cn => cn.Supplier)
            .Include(cn => cn.PurchaseReturn)
            .OrderByDescending(cn => cn.IssueDate);

        var totalCount = await query.CountAsync(cancellationToken);
        var creditNotes = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (creditNotes, totalCount);
    }

    public async Task<(IEnumerable<CreditNote> CreditNotes, int TotalCount)> SearchPagedAsync(
        CreditNoteQuery query, CancellationToken cancellationToken = default)
    {
        var dbQuery = dbContext.CreditNotes
            .Include(cn => cn.Supplier)
            .Include(cn => cn.PurchaseReturn)
            .AsQueryable();

        if (query.SupplierId.HasValue)
            dbQuery = dbQuery.Where(cn => cn.SupplierId == query.SupplierId.Value);

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

    public async Task AddAsync(CreditNote entity, CancellationToken cancellationToken = default)
    {
        await dbContext.CreditNotes.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(CreditNote entity, CancellationToken cancellationToken = default)
    {
        dbContext.CreditNotes.Update(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<decimal> GetTotalAvailableCreditAsync(Guid supplierId, CancellationToken cancellationToken = default)
    {
        return await dbContext.CreditNotes
            .Where(cn => cn.SupplierId == supplierId
                      && cn.Status != "CANCELLED"
                      && cn.Status != "EXPIRED"
                      && cn.Status != "FULLY_USED"
                      && (!cn.ExpiryDate.HasValue || cn.ExpiryDate.Value >= DateTime.UtcNow))
            .SumAsync(cn => cn.TotalAmount - cn.UsedAmount, cancellationToken);
    }
}
