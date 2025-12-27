using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly AutoPartDbContext _dbContext;

    public InvoiceRepository(AutoPartDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<Invoice>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Invoices
            .Where(x => !x.Isdeleted)
            .OrderByDescending(x => x.InvoiceDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Invoices
            .Include(x => x.SalesOrder)
            .Include(x => x.CustomerPayments)
            .FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task AddAsync(Invoice entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var exists = await _dbContext.Invoices
            .AnyAsync(x => x.InvoiceNumber == entity.InvoiceNumber && !x.Isdeleted, cancellationToken);

        if (exists)
            throw new InvalidOperationException($"Invoice with number '{entity.InvoiceNumber}' already exists");

        await _dbContext.Invoices.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Invoice entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _dbContext.Invoices.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Invoices
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity != null)
        {
            _dbContext.Invoices.Remove(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Invoices
            .AnyAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task<Invoice?> GetByNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Invoices
            .Include(x => x.SalesOrder)
            .Include(x => x.CustomerPayments)
            .FirstOrDefaultAsync(x => x.InvoiceNumber == invoiceNumber && !x.Isdeleted, cancellationToken);
    }

    public async Task<IEnumerable<Invoice>> GetBySalesOrderAsync(Guid salesOrderId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Invoices
            .Where(x => x.SalesOrderId == salesOrderId && !x.Isdeleted)
            .OrderByDescending(x => x.InvoiceDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Invoice>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Invoices
            .Where(x => x.Status == status && !x.Isdeleted)
            .OrderByDescending(x => x.InvoiceDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Invoice>> GetOverdueAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Invoices
            .Where(x => x.DueDate < DateTime.UtcNow && x.Status != "PAID" && x.Status != "CANCELLED" && !x.Isdeleted)
            .OrderByDescending(x => x.InvoiceDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<Invoice> invoices, int totalCount)> GetPagedAsync(int pageNumber, int pageSize,
        string? searchTerm = null,
        string? status = null,
        Guid? customerId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Invoices
            .Include(x => x.SalesOrder)
            .Include(x => x.CustomerPayments)
            .Where(x => !x.Isdeleted)
            .AsQueryable();

        // Apply search term (invoice number, sales order number, customer name, phone)
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var s = searchTerm.Trim().ToLower();
            query = query.Where(x => x.InvoiceNumber.ToLower().Contains(s)
                || (x.SalesOrder != null && x.SalesOrder.SONumber.ToLower().Contains(s))
                || (x.SalesOrder != null && x.SalesOrder.CustomerName.ToLower().Contains(s))
                || (x.SalesOrder != null && x.SalesOrder.CustomerPhone.ToLower().Contains(s)));
        }

        // Status filter
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        // Customer filter (by SalesOrder.CustomerId)
        if (customerId.HasValue && customerId != Guid.Empty)
        {
            query = query.Where(x => x.SalesOrder != null && x.SalesOrder.CustomerId == customerId.Value);
        }

        // Date range filters (inclusive)
        if (fromDate.HasValue)
        {
            var from = fromDate.Value.Date;
            query = query.Where(x => x.InvoiceDate >= from);
        }

        if (toDate.HasValue)
        {
            var to = toDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(x => x.InvoiceDate <= to);
        }

        query = query.OrderByDescending(x => x.InvoiceDate);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
