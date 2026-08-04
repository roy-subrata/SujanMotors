namespace AutoPartShop.Domain.Enums;

/// <summary>
/// Delivery channel for a <see cref="Entities.NotificationLog"/> entry / notification service call.
/// Member names are serialized as-is on the wire (global JsonStringEnumConverter, no naming
/// policy) — they must match the historical string literals exactly.
/// </summary>
public enum NotificationChannel
{
    SMS,
    WHATSAPP,
    EMAIL
}
