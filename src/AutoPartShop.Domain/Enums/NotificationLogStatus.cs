namespace AutoPartShop.Domain.Enums;

/// <summary>
/// Delivery status of a <see cref="Entities.NotificationLog"/> entry.
/// Member names are serialized as-is on the wire (global JsonStringEnumConverter, no naming
/// policy) — they must match the historical string literals exactly.
/// </summary>
public enum NotificationLogStatus
{
    PENDING,
    SENT,
    FAILED
}
