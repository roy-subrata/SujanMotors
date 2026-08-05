namespace AutoPartShop.Domain.Entities;

/// <summary>
/// A persistent, staff-facing notification shown in the inbox (e.g. consolidated low-stock
/// reorder alerts). Unlike <see cref="NotificationLog"/> — which is a delivery audit trail for
/// SMS/WhatsApp/email — an inbox notification is a human-readable message with read-state and
/// a deep-link into the UI so staff can act on it after the fact.
/// </summary>
public class InboxNotification : AuditableEntity
{
    public string Type { get; private set; } = string.Empty;  // REORDER_ALERT, ...
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string? PayloadJson { get; private set; }  // Serializable detail (e.g. low-stock items)
    public bool IsRead { get; private set; } = false;
    public DateTime? ReadAt { get; private set; }
    public string RouterLink { get; private set; } = string.Empty;  // Angular route to act on the alert
    public string? QueryParamsJson { get; private set; }

    private InboxNotification() { }

    public static InboxNotification Create(
        string type,
        string title,
        string message,
        string routerLink,
        string? payloadJson = null,
        string? queryParamsJson = null,
        string createdBy = "System")
    {
        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Type cannot be empty", nameof(type));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));

        var now = DateTime.UtcNow;
        return new InboxNotification
        {
            Type = type.Trim().ToUpper(),
            Title = title.Trim(),
            Message = message?.Trim() ?? string.Empty,
            PayloadJson = payloadJson,
            RouterLink = routerLink?.Trim() ?? string.Empty,
            QueryParamsJson = queryParamsJson,
            IsRead = false,
            CreatedDate = now,
            ModifiedDate = now,
            CreatedBy = createdBy,
            ModifiedBy = createdBy,
            Isdeleted = false
        };
    }

    public void MarkAsRead()
    {
        if (IsRead) return;

        IsRead = true;
        ReadAt = DateTime.UtcNow;
        ModifiedDate = DateTime.UtcNow;
    }

    public void MarkAsUnread()
    {
        if (!IsRead) return;

        IsRead = false;
        ReadAt = null;
        ModifiedDate = DateTime.UtcNow;
    }
}
