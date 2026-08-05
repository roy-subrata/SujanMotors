using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Domain.Repositories;

public interface IInboxNotificationRepository
{
    Task<InboxNotification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IEnumerable<InboxNotification> Items, int TotalCount)> GetPagedAsync(
        string? type, bool? unreadOnly, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(string? type = null, CancellationToken cancellationToken = default);
    Task<int> MarkAllAsReadAsync(string? type = null, CancellationToken cancellationToken = default);
    Task AddAsync(InboxNotification entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(InboxNotification entity, CancellationToken cancellationToken = default);
}
