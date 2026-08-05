using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class InboxNotificationRepository(AutoPartDbContext dbContext) : IInboxNotificationRepository
{
    public async Task<InboxNotification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.InboxNotifications
            .FirstOrDefaultAsync(n => n.Id == id && !n.Isdeleted, cancellationToken);
    }

    public async Task<(IEnumerable<InboxNotification> Items, int TotalCount)> GetPagedAsync(
        string? type, bool? unreadOnly, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.InboxNotifications.Where(n => !n.Isdeleted).AsQueryable();

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(n => n.Type == type.Trim().ToUpper());

        if (unreadOnly == true)
            query = query.Where(n => !n.IsRead);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(n => n.CreatedDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<int> GetUnreadCountAsync(string? type = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.InboxNotifications.Where(n => !n.Isdeleted && !n.IsRead).AsQueryable();

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(n => n.Type == type.Trim().ToUpper());

        return await query.CountAsync(cancellationToken);
    }

    public async Task<int> MarkAllAsReadAsync(string? type = null, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var query = dbContext.InboxNotifications.Where(n => !n.Isdeleted && !n.IsRead).AsQueryable();

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(n => n.Type == type.Trim().ToUpper());

        var updated = await query.ExecuteUpdateAsync(
            setters => setters
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, now)
                .SetProperty(n => n.ModifiedDate, now),
            cancellationToken);

        return updated;
    }

    public async Task AddAsync(InboxNotification entity, CancellationToken cancellationToken = default)
    {
        await dbContext.InboxNotifications.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(InboxNotification entity, CancellationToken cancellationToken = default)
    {
        dbContext.InboxNotifications.Update(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
