using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class TillSessionRepository(AutoPartDbContext dbContext) : ITillSessionRepository
{
    public async Task<TillSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.TillSessions
            .Include(t => t.Cashier)
            .Include(t => t.CashDrops)
            .FirstOrDefaultAsync(t => t.Id == id && !t.Isdeleted, cancellationToken);
    }

    public async Task<TillSession?> GetOpenSessionForCashierAsync(Guid cashierId, CancellationToken cancellationToken = default)
    {
        return await dbContext.TillSessions
            .Include(t => t.CashDrops)
            .FirstOrDefaultAsync(t => t.CashierId == cashierId && t.Status == "OPEN" && !t.Isdeleted, cancellationToken);
    }

    public async Task<TillSession?> GetLastClosedSessionForTerminalAsync(string terminalLabel, CancellationToken cancellationToken = default)
    {
        return await dbContext.TillSessions
            .Where(t => t.TerminalLabel == terminalLabel && t.Status == "CLOSED" && !t.Isdeleted)
            .OrderByDescending(t => t.ClosedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(IEnumerable<TillSession> Sessions, int TotalCount)> SearchPagedAsync(
        TillSessionQuery query, CancellationToken cancellationToken = default)
    {
        var dbQuery = dbContext.TillSessions
            .Include(t => t.Cashier)
            .Where(t => !t.Isdeleted)
            .AsQueryable();

        if (query.CashierId.HasValue)
            dbQuery = dbQuery.Where(t => t.CashierId == query.CashierId.Value);

        if (!string.IsNullOrWhiteSpace(query.Status))
            dbQuery = dbQuery.Where(t => t.Status == query.Status);

        if (query.FromDate.HasValue)
            dbQuery = dbQuery.Where(t => t.OpenedAt >= query.FromDate.Value);

        if (query.ToDate.HasValue)
            dbQuery = dbQuery.Where(t => t.OpenedAt <= query.ToDate.Value);

        var totalCount = await dbQuery.CountAsync(cancellationToken);
        var sessions = await dbQuery
            .OrderByDescending(t => t.OpenedAt)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (sessions, totalCount);
    }

    public async Task AddAsync(TillSession entity, CancellationToken cancellationToken = default)
    {
        await dbContext.TillSessions.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TillSession entity, CancellationToken cancellationToken = default)
    {
        dbContext.TillSessions.Update(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
