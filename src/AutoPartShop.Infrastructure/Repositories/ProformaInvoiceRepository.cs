using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class ProformaInvoiceRepository(AutoPartDbContext dbContext) : IProformaInvoiceRepository
{
    public async Task<ProformaInvoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.ProformaInvoices
            .Include(p => p.SalesOrder).ThenInclude(so => so!.Customer)
            .Include(p => p.SalesOrder).ThenInclude(so => so!.LineItems).ThenInclude(l => l.Part)
            .Include(p => p.SalesOrder).ThenInclude(so => so!.LineItems).ThenInclude(l => l.ProductVariant)
            .Include(p => p.SalesOrder).ThenInclude(so => so!.LineItems).ThenInclude(l => l.Unit)
            .FirstOrDefaultAsync(p => p.Id == id && !p.Isdeleted, cancellationToken);
    }

    public async Task<IEnumerable<ProformaInvoice>> GetBySalesOrderAsync(Guid salesOrderId, CancellationToken cancellationToken = default)
    {
        return await dbContext.ProformaInvoices
            .Where(p => p.SalesOrderId == salesOrderId && !p.Isdeleted)
            .OrderByDescending(p => p.IssueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<ProformaInvoice> ProformaInvoices, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.ProformaInvoices
            .Include(p => p.SalesOrder)
            .Where(p => !p.Isdeleted)
            .OrderByDescending(p => p.IssueDate);

        var totalCount = await query.CountAsync(cancellationToken);
        var proformas = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (proformas, totalCount);
    }

    public async Task AddAsync(ProformaInvoice entity, CancellationToken cancellationToken = default)
    {
        await dbContext.ProformaInvoices.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ProformaInvoice entity, CancellationToken cancellationToken = default)
    {
        dbContext.ProformaInvoices.Update(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
