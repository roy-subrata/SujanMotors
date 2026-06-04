using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class SalesReturnRepository : ISalesReturnRepository
{
    private readonly AutoPartDbContext _dbContext;

    public SalesReturnRepository(AutoPartDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<SalesReturn>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SalesReturns
            .Include(x => x.LineItems)
                .ThenInclude(li => li.Part)
            .Include(x => x.SalesOrder)
                .ThenInclude(so => so!.LineItems)
                    .ThenInclude(sol => sol.ProductVariant)
            .Where(x => !x.Isdeleted)
            .OrderByDescending(x => x.ReturnDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<SalesReturn?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SalesReturns
            .Include(x => x.LineItems)
                .ThenInclude(li => li.Part)
            .Include(x => x.SalesOrder)
                .ThenInclude(so => so!.LineItems)
                    .ThenInclude(sol => sol.ProductVariant)
            .FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task AddAsync(SalesReturn entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var exists = await _dbContext.SalesReturns
            .AnyAsync(x => x.ReturnNumber == entity.ReturnNumber && !x.Isdeleted, cancellationToken);

        if (exists)
            throw new InvalidOperationException($"Sales return with number '{entity.ReturnNumber}' already exists");

        await _dbContext.SalesReturns.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(SalesReturn entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _dbContext.SalesReturns.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.SalesReturns
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity != null)
        {
            _dbContext.SalesReturns.Remove(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SalesReturns
            .AnyAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task<SalesReturn?> GetByNumberAsync(string returnNumber, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SalesReturns
            .Include(x => x.LineItems)
                .ThenInclude(li => li.Part)
            .Include(x => x.SalesOrder)
                .ThenInclude(so => so!.LineItems)
                    .ThenInclude(sol => sol.ProductVariant)
            .FirstOrDefaultAsync(x => x.ReturnNumber == returnNumber && !x.Isdeleted, cancellationToken);
    }

    public async Task<IEnumerable<SalesReturn>> GetBySalesOrderAsync(Guid salesOrderId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SalesReturns
            .Include(x => x.LineItems)
                .ThenInclude(li => li.Part)
            .Include(x => x.SalesOrder)
                .ThenInclude(so => so!.LineItems)
                    .ThenInclude(sol => sol.ProductVariant)
            .Where(x => x.SalesOrderId == salesOrderId && !x.Isdeleted)
            .OrderByDescending(x => x.ReturnDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SalesReturn>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SalesReturns
            .Include(x => x.LineItems)
                .ThenInclude(li => li.Part)
            .Include(x => x.SalesOrder)
                .ThenInclude(so => so!.LineItems)
                    .ThenInclude(sol => sol.ProductVariant)
            .Where(x => x.Status == status && !x.Isdeleted)
            .OrderByDescending(x => x.ReturnDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SalesReturn>> GetPendingApprovalAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SalesReturns
            .Include(x => x.LineItems)
                .ThenInclude(li => li.Part)
            .Include(x => x.SalesOrder)
                .ThenInclude(so => so!.LineItems)
                    .ThenInclude(sol => sol.ProductVariant)
            .Where(x => x.Status == "PENDING" && !x.Isdeleted)
            .OrderByDescending(x => x.ReturnDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<SalesReturn> returns, int totalCount)> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        IQueryable<SalesReturn> query = _dbContext.SalesReturns
            .Include(x => x.LineItems)
                .ThenInclude(li => li.Part)
            .Include(x => x.SalesOrder)
                .ThenInclude(so => so!.LineItems)
                    .ThenInclude(sol => sol.ProductVariant)
            .Where(x => !x.Isdeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(x =>
                x.ReturnNumber.ToLower().Contains(term) ||
                x.Reason.ToLower().Contains(term) ||
                (x.SalesOrder != null && (
                    x.SalesOrder.SONumber.ToLower().Contains(term) ||
                    x.SalesOrder.CustomerName.ToLower().Contains(term)
                )));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.ReturnDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
