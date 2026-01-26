using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class SalesOrderRepository : ISalesOrderRepository
{
    private readonly AutoPartDbContext _dbContext;

    public SalesOrderRepository(AutoPartDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<SalesOrder>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SalesOrders
            .Include(x => x.LineItems)
                .ThenInclude(li => li.Part)
            .Where(x => !x.Isdeleted)
            .OrderByDescending(x => x.SODate)
            .ToListAsync(cancellationToken);
    }

    public async Task<SalesOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SalesOrders
            .Include(x => x.LineItems)
                .ThenInclude(li => li.Part)
            .FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task AddAsync(SalesOrder entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var exists = await _dbContext.SalesOrders
            .AnyAsync(x => x.SONumber == entity.SONumber && !x.Isdeleted, cancellationToken);

        if (exists)
            throw new InvalidOperationException($"Sales order with number '{entity.SONumber}' already exists");

        await _dbContext.SalesOrders.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(SalesOrder entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _dbContext.SalesOrders.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.SalesOrders
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity != null)
        {
            _dbContext.SalesOrders.Remove(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SalesOrders
            .AnyAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task<SalesOrder?> GetByNumberAsync(string soNumber, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SalesOrders
            .Include(x => x.LineItems)
                .ThenInclude(li => li.Part)
            .FirstOrDefaultAsync(x => x.SONumber == soNumber && !x.Isdeleted, cancellationToken);
    }

    public async Task<IEnumerable<SalesOrder>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SalesOrders
            .Include(x => x.LineItems)
                .ThenInclude(li => li.Part)
            .Where(x => x.CustomerId == customerId && !x.Isdeleted)
            .OrderByDescending(x => x.SODate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SalesOrder>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SalesOrders
            .Include(x => x.LineItems)
                .ThenInclude(li => li.Part)
            .Where(x => x.Status == status && !x.Isdeleted)
            .OrderByDescending(x => x.SODate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SalesOrder>> GetOverdueAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SalesOrders
            .Include(x => x.LineItems)
                .ThenInclude(li => li.Part)
            .Where(x => x.DeliveryDate.HasValue && x.DeliveryDate < DateTime.UtcNow && x.Status != "DELIVERED" && x.Status != "CANCELLED" && !x.Isdeleted)
            .OrderByDescending(x => x.SODate)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<SalesOrder> orders, int totalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SalesOrders
            .Include(x => x.LineItems)
                .ThenInclude(li => li.Part)
            .Where(x => !x.Isdeleted)
            .OrderByDescending(x => x.SODate);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IEnumerable<SalesOrder> orders, int totalCount)> SearchPagedAsync(string searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var term = searchTerm.ToLower();
        var query = _dbContext.SalesOrders
            .Include(x => x.LineItems)
                .ThenInclude(li => li.Part)
            .Where(x => !x.Isdeleted && (
                x.SONumber.ToLower().Contains(term) ||
                x.CustomerName.ToLower().Contains(term) ||
                x.CustomerEmail.ToLower().Contains(term) ||
                x.CustomerPhone.ToLower().Contains(term)
            ))
            .OrderByDescending(x => x.SODate);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
